using System.Collections.Concurrent;
using PlanningPoker.Api.Models;

namespace PlanningPoker.Api.Services
{
    public class VotingSessionService
    {
        // In-memory storage for voting sessions
        private readonly ConcurrentDictionary<string, VotingSession> _sessions = new();

        public VotingSession GetOrCreateSession(string roomId)
        {
            return _sessions.GetOrAdd(roomId, _ => new VotingSession
            {
                RoomId = roomId,
                Mode = VotingMode.Start
            });
        }

        public VotingSession? GetSession(string roomId)
        {
            _sessions.TryGetValue(roomId, out var session);
            return session;
        }

        public void AddParticipant(string roomId, string username, string connectionId)
        {
            var session = GetOrCreateSession(roomId);
            lock (session)
            {
                session.Participants[username] = new VotingParticipant
                {
                    Username = username,
                    ConnectionId = connectionId,
                    Vote = null
                };
            }
        }

        public void RemoveParticipant(string roomId, string username)
        {
            var session = GetSession(roomId);
            if (session != null)
            {
                lock (session)
                {
                    session.Participants.Remove(username);
                }
            }
        }

        public void RemoveParticipantByConnectionId(string roomId, string connectionId)
        {
            var session = GetSession(roomId);
            if (session != null)
            {
                lock (session)
                {
                    var participant = session.Participants.Values.FirstOrDefault(p => p.ConnectionId == connectionId);
                    if (participant != null)
                    {
                        session.Participants.Remove(participant.Username);
                    }
                }
            }
        }

        public bool StartVoting(string roomId, string storyName, List<string>? cardValues, int? timerSeconds)
        {
            var session = GetSession(roomId);
            if (session == null || session.Mode != VotingMode.Start && session.Mode != VotingMode.Results)
                return false;

            lock (session)
            {
                session.Mode = VotingMode.Voting;
                session.StoryName = storyName;
                session.CardValues = cardValues;
                session.TimerSeconds = timerSeconds;
                session.VotingStartTime = DateTime.UtcNow;
                
                // Clear previous votes
                foreach (var participant in session.Participants.Values)
                {
                    participant.Vote = null;
                }
            }

            return true;
        }

        public bool SubmitVote(string roomId, string username, string cardValue)
        {
            var session = GetSession(roomId);
            if (session == null || session.Mode != VotingMode.Voting)
                return false;

            lock (session)
            {
                if (!session.Participants.TryGetValue(username, out var participant))
                    return false;

                // Lock the vote - cannot change once submitted
                if (participant.Vote != null)
                    return false;

                participant.Vote = cardValue;
            }

            return true;
        }

        public bool ShouldRevealResults(string roomId)
        {
            var session = GetSession(roomId);
            if (session == null || session.Mode != VotingMode.Voting)
                return false;

            lock (session)
            {
                // Auto-reveal if all users voted
                if (session.AllUsersVoted())
                    return true;

                // Auto-reveal if timer expired
                if (session.TimerSeconds.HasValue && session.VotingStartTime.HasValue)
                {
                    var elapsed = (DateTime.UtcNow - session.VotingStartTime.Value).TotalSeconds;
                    if (elapsed >= session.TimerSeconds.Value)
                        return true;
                }
            }

            return false;
        }

        public VotingResults? RevealResults(string roomId)
        {
            var session = GetSession(roomId);
            if (session == null || session.Mode != VotingMode.Voting)
                return null;

            lock (session)
            {
                session.Mode = VotingMode.Results;
                return session.CalculateResults();
            }
        }

        public List<VotingParticipant> GetParticipants(string roomId)
        {
            var session = GetSession(roomId);
            if (session == null)
                return new List<VotingParticipant>();

            lock (session)
            {
                return session.Participants.Values.ToList();
            }
        }

        public string? FindRoomByConnectionId(string connectionId)
        {
            foreach (var kvp in _sessions)
            {
                var session = kvp.Value;
                lock (session)
                {
                    if (session.Participants.Values.Any(p => p.ConnectionId == connectionId))
                    {
                        return kvp.Key;
                    }
                }
            }
            return null;
        }
    }
}
