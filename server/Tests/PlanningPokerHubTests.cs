using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.SignalR;
using PlanningPoker.Api.Hubs;
using PlanningPoker.Api.Services;
using PlanningPoker.Api.Repositories;
using PlanningPoker.Api.Models;

namespace PlanningPoker.Api.Tests
{
    public class PlanningPokerHubTests
    {
        private readonly Mock<VotingSessionService> _mockVotingService;
        private readonly Mock<IRoomRepository> _mockRoomRepository;
        private readonly Mock<TokenService> _mockTokenService;
        private readonly Mock<IHubCallerClients> _mockClients;
        private readonly Mock<HubCallerContext> _mockContext;
        private readonly Mock<IGroupManager> _mockGroups;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly PlanningPokerHub _hub;

        public PlanningPokerHubTests()
        {
            _mockVotingService = new Mock<VotingSessionService>();
            _mockRoomRepository = new Mock<IRoomRepository>();
            _mockTokenService = new Mock<TokenService>();
            _mockClients = new Mock<IHubCallerClients>();
            _mockContext = new Mock<HubCallerContext>();
            _mockGroups = new Mock<IGroupManager>();
            _mockClientProxy = new Mock<IClientProxy>();

            _hub = new PlanningPokerHub(
                _mockVotingService.Object,
                _mockRoomRepository.Object,
                _mockTokenService.Object)
            {
                Clients = _mockClients.Object,
                Context = _mockContext.Object,
                Groups = _mockGroups.Object
            };

            // Setup default mocks
            _mockContext.Setup(c => c.ConnectionId).Returns("test-connection-id");
            _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        }

        [Fact]
        public async Task JoinRoom_WithValidToken_AddsUserToRoom()
        {
            // Arrange
            var roomId = "room-123";
            var token = "valid-token";
            var user = new User { Username = "TestUser", Password = "test", Role = UserRole.User };
            var room = new RoomConfig 
            { 
                RoomId = roomId, 
                Name = "Test Room", 
                HostUsername = "HostUser",
                PokerCards = new List<string> { "1", "2", "3" },
                VotingCountdownSeconds = 60
            };

            _mockTokenService.Setup(s => s.ValidateToken(token)).Returns(user);
            _mockRoomRepository.Setup(r => r.Get(roomId)).Returns(room);
            _mockVotingService.Setup(s => s.GetSession(roomId)).Returns(new VotingSession 
            { 
                RoomId = roomId, 
                Mode = VotingMode.Start,
                Participants = new Dictionary<string, VotingParticipant>()
            });

            // Act
            var result = await _hub.JoinRoom(roomId, token);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(roomId, result.RoomId);
            Assert.Equal("Test Room", result.RoomName);
            _mockVotingService.Verify(s => s.AddParticipant(roomId, "TestUser", "test-connection-id"), Times.Once);
            _mockGroups.Verify(g => g.AddToGroupAsync("test-connection-id", roomId, default), Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync("UserJoined", It.Is<object[]>(o => o.Length == 1 && (string)o[0] == "TestUser"), default),
                Times.Once);
        }

        [Fact]
        public async Task JoinRoom_WithInvalidToken_ThrowsException()
        {
            // Arrange
            var roomId = "room-123";
            var token = "invalid-token";

            _mockTokenService.Setup(s => s.ValidateToken(token)).Returns((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<HubException>(() => _hub.JoinRoom(roomId, token));
        }

        [Fact]
        public async Task JoinRoom_WithNonExistentRoom_ThrowsException()
        {
            // Arrange
            var roomId = "non-existent-room";
            var token = "valid-token";
            var user = new User { Username = "TestUser", Password = "test", Role = UserRole.User };

            _mockTokenService.Setup(s => s.ValidateToken(token)).Returns(user);
            _mockRoomRepository.Setup(r => r.Get(roomId)).Returns((RoomConfig?)null);

            // Act & Assert
            await Assert.ThrowsAsync<HubException>(() => _hub.JoinRoom(roomId, token));
        }

        [Fact]
        public async Task StartVoting_ByHost_StartsVotingSuccessfully()
        {
            // Arrange
            var roomId = "room-123";
            var hostUsername = "HostUser";
            var room = new RoomConfig 
            { 
                RoomId = roomId, 
                Name = "Test Room", 
                HostUsername = hostUsername,
                PokerCards = new List<string> { "1", "2", "3" },
                VotingCountdownSeconds = 60
            };
            var request = new StartVotingRequest 
            { 
                StoryName = "Test Story",
                CardValues = new List<string> { "1", "2", "3" },
                TimerSeconds = 60
            };

            _mockRoomRepository.Setup(r => r.Get(roomId)).Returns(room);
            _mockVotingService.Setup(s => s.GetParticipants(roomId))
                .Returns(new List<VotingParticipant> 
                {
                    new VotingParticipant { Username = hostUsername, ConnectionId = "test-connection-id" }
                });
            _mockVotingService.Setup(s => s.StartVoting(roomId, request.StoryName, request.CardValues, request.TimerSeconds))
                .Returns(true);
            _mockVotingService.Setup(s => s.GetSession(roomId)).Returns(new VotingSession
            {
                RoomId = roomId,
                Mode = VotingMode.Voting,
                StoryName = request.StoryName,
                CardValues = request.CardValues,
                TimerSeconds = request.TimerSeconds,
                VotingStartTime = DateTime.UtcNow,
                Participants = new Dictionary<string, VotingParticipant>()
            });

            // Act
            var result = await _hub.StartVoting(roomId, request);

            // Assert
            Assert.True(result);
            _mockVotingService.Verify(s => s.StartVoting(roomId, request.StoryName, request.CardValues, request.TimerSeconds), Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync("VotingStarted", It.IsAny<object[]>(), default),
                Times.Once);
        }

        [Fact]
        public async Task StartVoting_ByNonHost_ThrowsException()
        {
            // Arrange
            var roomId = "room-123";
            var hostUsername = "HostUser";
            var nonHostUsername = "NonHostUser";
            var room = new RoomConfig 
            { 
                RoomId = roomId, 
                Name = "Test Room", 
                HostUsername = hostUsername,
                PokerCards = new List<string> { "1", "2", "3" },
                VotingCountdownSeconds = 60
            };
            var request = new StartVotingRequest { StoryName = "Test Story" };

            _mockRoomRepository.Setup(r => r.Get(roomId)).Returns(room);
            _mockVotingService.Setup(s => s.GetParticipants(roomId))
                .Returns(new List<VotingParticipant> 
                {
                    new VotingParticipant { Username = nonHostUsername, ConnectionId = "test-connection-id" }
                });

            // Act & Assert
            await Assert.ThrowsAsync<HubException>(() => _hub.StartVoting(roomId, request));
        }

        [Fact]
        public async Task SubmitVote_InVotingMode_SubmitsSuccessfully()
        {
            // Arrange
            var roomId = "room-123";
            var username = "TestUser";
            var request = new SubmitVoteRequest { CardValue = "3" };

            _mockVotingService.Setup(s => s.GetParticipants(roomId))
                .Returns(new List<VotingParticipant> 
                {
                    new VotingParticipant { Username = username, ConnectionId = "test-connection-id" }
                });
            _mockVotingService.Setup(s => s.SubmitVote(roomId, username, request.CardValue))
                .Returns(true);
            _mockVotingService.Setup(s => s.ShouldRevealResults(roomId))
                .Returns(false);

            // Act
            var result = await _hub.SubmitVote(roomId, request);

            // Assert
            Assert.True(result);
            _mockVotingService.Verify(s => s.SubmitVote(roomId, username, request.CardValue), Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync("UserVoted", It.Is<object[]>(o => o.Length == 1 && (string)o[0] == username), default),
                Times.Once);
        }

        [Fact]
        public async Task SubmitVote_WhenAllVoted_TriggersReveal()
        {
            // Arrange
            var roomId = "room-123";
            var username = "TestUser";
            var request = new SubmitVoteRequest { CardValue = "3" };
            var room = new RoomConfig 
            { 
                RoomId = roomId, 
                Name = "Test Room", 
                HostUsername = "HostUser",
                PokerCards = new List<string> { "1", "2", "3" },
                VotingCountdownSeconds = 60
            };
            var results = new VotingResults 
            { 
                StoryName = "Test Story",
                UserVotes = new Dictionary<string, string> { { username, "3" } },
                AverageScore = 3.0
            };

            _mockVotingService.Setup(s => s.GetParticipants(roomId))
                .Returns(new List<VotingParticipant> 
                {
                    new VotingParticipant { Username = username, ConnectionId = "test-connection-id" }
                });
            _mockVotingService.Setup(s => s.SubmitVote(roomId, username, request.CardValue))
                .Returns(true);
            _mockVotingService.Setup(s => s.ShouldRevealResults(roomId))
                .Returns(true);
            _mockVotingService.Setup(s => s.RevealResults(roomId))
                .Returns(results);
            _mockRoomRepository.Setup(r => r.Get(roomId)).Returns(room);
            _mockVotingService.Setup(s => s.GetSession(roomId)).Returns(new VotingSession
            {
                RoomId = roomId,
                Mode = VotingMode.Results,
                Participants = new Dictionary<string, VotingParticipant>()
            });

            // Act
            var result = await _hub.SubmitVote(roomId, request);

            // Assert
            Assert.True(result);
            _mockVotingService.Verify(s => s.RevealResults(roomId), Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync("ResultsRevealed", It.IsAny<object[]>(), default),
                Times.Once);
        }

        [Fact]
        public async Task LeaveRoom_RemovesUserAndNotifiesOthers()
        {
            // Arrange
            var roomId = "room-123";
            var username = "TestUser";

            _mockVotingService.Setup(s => s.GetParticipants(roomId))
                .Returns(new List<VotingParticipant> 
                {
                    new VotingParticipant { Username = username, ConnectionId = "test-connection-id" }
                });

            // Act
            await _hub.LeaveRoom(roomId);

            // Assert
            _mockVotingService.Verify(s => s.RemoveParticipant(roomId, username), Times.Once);
            _mockGroups.Verify(g => g.RemoveFromGroupAsync("test-connection-id", roomId, default), Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync("UserLeft", It.Is<object[]>(o => o.Length == 1 && (string)o[0] == username), default),
                Times.Once);
        }

        [Fact]
        public async Task OnDisconnectedAsync_RemovesUserAndNotifiesRoom()
        {
            // Arrange
            var roomId = "room-123";
            var username = "TestUser";

            _mockVotingService.Setup(s => s.FindRoomByConnectionId("test-connection-id"))
                .Returns(roomId);
            _mockVotingService.Setup(s => s.GetParticipants(roomId))
                .Returns(new List<VotingParticipant> 
                {
                    new VotingParticipant { Username = username, ConnectionId = "test-connection-id" }
                });

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            _mockVotingService.Verify(s => s.RemoveParticipantByConnectionId(roomId, "test-connection-id"), Times.Once);
            _mockClientProxy.Verify(
                c => c.SendCoreAsync("UserLeft", It.Is<object[]>(o => o.Length == 1 && (string)o[0] == username), default),
                Times.Once);
        }
    }
}
