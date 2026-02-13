using Microsoft.AspNetCore.SignalR;
using PlanningPoker.Api.Models;
using PlanningPoker.Api.Repositories;
using PlanningPoker.Api.Services;

namespace PlanningPoker.Api.Hubs
{
    public class PlanningPokerHub : Hub
    {
        private readonly VotingSessionService _votingService;
        private readonly IRoomRepository _roomRepository;
        private readonly TokenService _tokenService;
        private readonly IHubContext<PlanningPokerHub> _hubContext;

        public PlanningPokerHub(
            VotingSessionService votingService,
            IRoomRepository roomRepository,
            TokenService tokenService,
            IHubContext<PlanningPokerHub> hubContext)
        {
            _votingService = votingService;
            _roomRepository = roomRepository;
            _tokenService = tokenService;
            _hubContext = hubContext;
        }

        public async Task<RoomStateDto?> JoinRoom(string roomId, string token)
        {
            // Validate token
            var user = _tokenService.ValidateToken(token);
            if (user == null)
            {
                throw new HubException("Unauthorized");
            }

            // Get room config from database
            var room = _roomRepository.Get(roomId);
            if (room == null)
            {
                throw new HubException("Room not found");
            }

            // Add user to SignalR group for this room
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // Add participant to voting session
            _votingService.AddParticipant(roomId, user.Username, Context.ConnectionId);

            // Notify other users in the room
            await Clients.GroupExcept(roomId, Context.ConnectionId).SendAsync("UserJoined", user.Username);

            // Return current room state to the joining user
            return await GetRoomState(roomId);
        }

        public async Task LeaveRoom(string roomId)
        {
            var username = GetUsernameFromConnection(roomId);
            if (username == null) return;

            _votingService.RemoveParticipant(roomId, username);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("UserLeft", username);
        }

        public async Task<bool> StartVoting(string roomId, StartVotingRequest request)
        {
            var username = GetUsernameFromConnection(roomId);
            var room = _roomRepository.Get(roomId);
            
            if (username == null || room == null || room.HostUsername != username)
            {
                throw new HubException("Only the host can start voting");
            }

            // Use request values directly, use room defaults only if request doesn't provide them
            var cardValues = request.CardValues ?? room.PokerCards;
            var timerSeconds = request.TimerSeconds; // Use null if not provided, don't fall back to room default

            var success = _votingService.StartVoting(roomId, request.StoryName, cardValues, timerSeconds);
            
            if (success)
            {
                var state = await GetRoomState(roomId);
                await Clients.Group(roomId).SendAsync("VotingStarted", state);

                // If there's a timer, schedule result reveal
                if (timerSeconds.HasValue)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(timerSeconds.Value));
                            
                            // Check if session is still in Voting mode before revealing
                            var currentSession = _votingService.GetSession(roomId);
                            if (currentSession?.Mode == VotingMode.Voting)
                            {
                                // Reveal results
                                if (_votingService.ShouldRevealResults(roomId))
                                {
                                    var timerResults = _votingService.RevealResults(roomId);
                                    if (timerResults != null)
                                    {
                                        var timerState = await GetRoomState(roomId);
                                        // Use _hubContext to send from background task
                                        await _hubContext.Clients.Group(roomId).SendAsync("ResultsRevealed", timerState);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in timer task for room {roomId}: {ex.Message}");
                        }
                    });
                }
            }

            return success;
        }

        public async Task<bool> SubmitVote(string roomId, SubmitVoteRequest request)
        {
            var username = GetUsernameFromConnection(roomId);
            if (username == null)
            {
                throw new HubException("User not in room");
            }

            var success = _votingService.SubmitVote(roomId, username, request.CardValue);
            
            if (success)
            {
                // Notify all users that this user has voted (but don't reveal the vote yet)
                await Clients.Group(roomId).SendAsync("UserVoted", username);

                // Check if we should auto-reveal results
                await CheckAndRevealResults(roomId);
            }

            return success;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var roomId = _votingService.FindRoomByConnectionId(Context.ConnectionId);
            if (roomId != null)
            {
                var username = GetUsernameFromConnection(roomId);
                if (username != null)
                {
                    _votingService.RemoveParticipantByConnectionId(roomId, Context.ConnectionId);
                    await Clients.Group(roomId).SendAsync("UserLeft", username);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        private async Task CheckAndRevealResults(string roomId)
        {
            if (_votingService.ShouldRevealResults(roomId))
            {
                var results = _votingService.RevealResults(roomId);
                if (results != null)
                {
                    var state = await GetRoomState(roomId);
                    await Clients.Group(roomId).SendAsync("ResultsRevealed", state);
                }
            }
        }

        private async Task<RoomStateDto> GetRoomState(string roomId)
        {
            var room = _roomRepository.Get(roomId);
            var session = _votingService.GetSession(roomId);
            
            if (room == null)
            {
                throw new HubException("Room not found");
            }

            var participants = session?.Participants.Values.ToList() ?? new List<VotingParticipant>();
            var mode = session?.Mode ?? VotingMode.Start;

            var participantDtos = participants.Select(p => new ParticipantDto
            {
                Username = p.Username,
                HasVoted = mode == VotingMode.Voting && p.Vote != null,
                Vote = mode == VotingMode.Results ? (p.Vote ?? "?") : null
            }).ToList();

            // Use session timer if in voting, otherwise use room default for form pre-fill
            var timerSeconds = mode == VotingMode.Voting 
                ? session?.TimerSeconds 
                : (room.VotingCountdownSeconds > 0 ? room.VotingCountdownSeconds : (int?)null);

            return new RoomStateDto
            {
                RoomId = roomId,
                RoomName = room.Name,
                HostUsername = room.HostUsername,
                Mode = mode,
                Participants = participantDtos,
                StoryName = session?.StoryName,
                CardValues = session?.CardValues ?? room.PokerCards,
                TimerSeconds = timerSeconds,
                VotingStartTime = session?.VotingStartTime,
                Results = mode == VotingMode.Results && session != null ? session.CalculateResults() : null
            };
        }

        private string? GetUsernameFromConnection(string roomId)
        {
            var participants = _votingService.GetParticipants(roomId);
            return participants.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId)?.Username;
        }
    }
}
