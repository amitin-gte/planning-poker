using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using PlanningPoker.Api.Services;
using PlanningPoker.Api.Models;

namespace PlanningPoker.Api.Tests
{
    public class VotingSessionServiceTests
    {
        [Fact]
        public void GetOrCreateSession_CreatesNewSessionInStartMode()
        {
            // Arrange
            var service = new VotingSessionService();
            var roomId = "test-room-1";

            // Act
            var session = service.GetOrCreateSession(roomId);

            // Assert
            Assert.NotNull(session);
            Assert.Equal(roomId, session.RoomId);
            Assert.Equal(VotingMode.Start, session.Mode);
            Assert.Empty(session.Participants);
        }

        [Fact]
        public void AddParticipant_AddsUserToSession()
        {
            // Arrange
            var service = new VotingSessionService();
            var roomId = "test-room-1";
            var username = "TestUser";
            var connectionId = "conn-123";

            // Act
            service.AddParticipant(roomId, username, connectionId);
            var session = service.GetSession(roomId);

            // Assert
            Assert.NotNull(session);
            Assert.Single(session.Participants);
            Assert.True(session.Participants.ContainsKey(username));
            Assert.Equal(connectionId, session.Participants[username].ConnectionId);
            Assert.Null(session.Participants[username].Vote);
        }

        [Fact]
        public void RemoveParticipant_RemovesUserFromSession()
        {
            // Arrange
            var service = new VotingSessionService();
            var roomId = "test-room-1";
            var username = "TestUser";
            service.AddParticipant(roomId, username, "conn-123");

            // Act
            service.RemoveParticipant(roomId, username);
            var session = service.GetSession(roomId);

            // Assert
            Assert.NotNull(session);
            Assert.Empty(session.Participants);
        }

        [Fact]
        public void StartVoting_TransitionsFromStartToVoting()
        {
            // Arrange
            var service = new VotingSessionService();
            var roomId = "test-room-1";
            service.GetOrCreateSession(roomId);
            var storyName = "User Story 1";
            var cards = new List<string> { "1", "2", "3", "5", "8" };
            var timer = 60;

            // Act
            var result = service.StartVoting(roomId, storyName, cards, timer);
            var session = service.GetSession(roomId);

            // Assert
            Assert.True(result);
            Assert.NotNull(session);
            Assert.Equal(VotingMode.Voting, session.Mode);
            Assert.Equal(storyName, session.StoryName);
            Assert.Equal(cards, session.CardValues);
            Assert.Equal(timer, session.TimerSeconds);
            Assert.NotNull(session.VotingStartTime);
        }

        [Fact]
        public void StartVoting_FromResultsMode_TransitionsToVoting()
        {
            // Arrange
            var service = new VotingSessionService();
            var roomId = "test-room-1";
            service.GetOrCreateSession(roomId);
            service.AddParticipant(roomId, "User1", "conn-1");
            service.StartVoting(roomId, "Story 1", new List<string> { "1", "2", "3" }, 60);
            service.SubmitVote(roomId, "User1", "2");
            service.RevealResults(roomId);

            // Act
            var result = service.StartVoting(roomId, "Story 2", new List<string> { "1", "2", "3" }, 60);
            var session = service.GetSession(roomId);

            // Assert
            Assert.True(result);
            Assert.Equal(VotingMode.Voting, session!.Mode);
            Assert.Equal("Story 2", session.StoryName);
            Assert.Null(session.Participants["User1"].Vote); // Vote should be cleared
        }

        [Fact]
        public void StartVoting_ClearsPreviousVotes()
        {
            // Arrange
            var service = new VotingSessionService();
            var roomId = "test-room-1";
            service.GetOrCreateSession(roomId);
            service.AddParticipant(roomId, "User1", "conn-1");
            service.StartVoting(roomId, "Story 1", new List<string> { "1", "2" }, 60);
            service.SubmitVote(roomId, "User1", "2");
            service.RevealResults(roomId);

            // Act
            service.StartVoting(roomId, "Story 2", new List<string> { "1", "2" }, 60);
            var session = service.GetSession(roomId);

            // Assert
            Assert.Null(session!.Participants["User1"].Vote);
        }

        [Fact]
        public void SubmitVote_LocksVote()
        {
            // Arrange
            var service = new VotingSessionService();
            var roomId = "test-room-1";
            service.GetOrCreateSession(roomId);
            service.AddParticipant(roomId, "User1", "conn-1");
            service.StartVoting(roomId, "Story 1", new List<string> { "1", "2", "3" }, 60);

            // Act
            var result1 = service.SubmitVote(roomId, "User1", "2");
            var result2 = service.SubmitVote(roomId, "User1", "3"); // Try to change vote
            var session = service.GetSession(roomId);

            // Assert
            Assert.True(result1);
            Assert.False(result2); // Should not allow changing vote
            Assert.Equal("2", session!.Participants["User1"].Vote);
        }

        [Fact]
        public void SubmitVote_OnlyWorksInVotingMode()
        {
            // Arrange
            var service = new VotingSessionService();
            var roomId = "test-room-1";
            service.GetOrCreateSession(roomId);
            service.AddParticipant(roomId, "User1", "conn-1");

            // Act
            var result = service.SubmitVote(roomId, "User1", "2");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldRevealResults_WhenAllUsersVoted()
        {
            // Arrange
            var service = new VotingSessionService();
            var roomId = "test-room-1";
            service.GetOrCreateSession(roomId);
            service.AddParticipant(roomId, "User1", "conn-1");
            service.AddParticipant(roomId, "User2", "conn-2");
            service.StartVoting(roomId, "Story 1", new List<string> { "1", "2", "3" }, 60);

            // Act
            service.SubmitVote(roomId, "User1", "2");
            var shouldReveal1 = service.ShouldRevealResults(roomId);
            service.SubmitVote(roomId, "User2", "3");
            var shouldReveal2 = service.ShouldRevealResults(roomId);

            // Assert
            Assert.False(shouldReveal1);
            Assert.True(shouldReveal2);
        }

        [Fact]
        public void ShouldRevealResults_WhenTimerExpires()
        {
            // Arrange
            var service = new VotingSessionService();
            var roomId = "test-room-1";
            service.GetOrCreateSession(roomId);
            service.AddParticipant(roomId, "User1", "conn-1");
            service.StartVoting(roomId, "Story 1", new List<string> { "1", "2", "3" }, 1); // 1 second timer

            // Act
            var shouldRevealBefore = service.ShouldRevealResults(roomId);
            Thread.Sleep(1100); // Wait for timer to expire
            var shouldRevealAfter = service.ShouldRevealResults(roomId);

            // Assert
            Assert.False(shouldRevealBefore);
            Assert.True(shouldRevealAfter);
        }

        [Fact]
        public void RevealResults_CalculatesAverageCorrectly()
        {
            // Arrange
            var service = new VotingSessionService();
            var roomId = "test-room-1";
            service.GetOrCreateSession(roomId);
            service.AddParticipant(roomId, "User1", "conn-1");
            service.AddParticipant(roomId, "User2", "conn-2");
            service.AddParticipant(roomId, "User3", "conn-3");
            service.StartVoting(roomId, "Story 1", new List<string> { "1", "2", "3", "5", "8" }, 60);
            service.SubmitVote(roomId, "User1", "2");
            service.SubmitVote(roomId, "User2", "3");
            service.SubmitVote(roomId, "User3", "5");

            // Act
            var results = service.RevealResults(roomId);

            // Assert
            Assert.NotNull(results);
            Assert.Equal("Story 1", results.StoryName);
            Assert.Equal(3, results.UserVotes.Count);
            Assert.NotNull(results.AverageScore);
            Assert.Equal(3.33, results.AverageScore.Value, 2); // (2+3+5)/3 = 3.33
        }

        [Fact]
        public void RevealResults_ExcludesNonNumericVotes()
        {
            // Arrange
            var service = new VotingSessionService();
            var roomId = "test-room-1";
            service.GetOrCreateSession(roomId);
            service.AddParticipant(roomId, "User1", "conn-1");
            service.AddParticipant(roomId, "User2", "conn-2");
            service.AddParticipant(roomId, "User3", "conn-3");
            service.StartVoting(roomId, "Story 1", new List<string> { "1", "2", "3", "?", "∞" }, 60);
            service.SubmitVote(roomId, "User1", "2");
            service.SubmitVote(roomId, "User2", "?");
            service.SubmitVote(roomId, "User3", "3");

            // Act
            var results = service.RevealResults(roomId);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2.5, results.AverageScore); // (2+3)/2 = 2.5
            Assert.Equal("?", results.UserVotes["User2"]);
        }

        [Fact]
        public void RevealResults_HandlesNoVotes()
        {
            // Arrange
            var service = new VotingSessionService();
            var roomId = "test-room-1";
            service.GetOrCreateSession(roomId);
            service.AddParticipant(roomId, "User1", "conn-1");
            service.AddParticipant(roomId, "User2", "conn-2");
            service.StartVoting(roomId, "Story 1", new List<string> { "1", "2", "3" }, 60);

            // Act
            var results = service.RevealResults(roomId);

            // Assert
            Assert.NotNull(results);
            Assert.Equal("?", results.UserVotes["User1"]);
            Assert.Equal("?", results.UserVotes["User2"]);
            Assert.Null(results.AverageScore);
        }

        [Fact]
        public void FindRoomByConnectionId_ReturnsCorrectRoom()
        {
            // Arrange
            var service = new VotingSessionService();
            var roomId = "test-room-1";
            var connectionId = "conn-123";
            service.AddParticipant(roomId, "User1", connectionId);

            // Act
            var foundRoomId = service.FindRoomByConnectionId(connectionId);

            // Assert
            Assert.Equal(roomId, foundRoomId);
        }

        [Fact]
        public void RemoveParticipantByConnectionId_RemovesCorrectUser()
        {
            // Arrange
            var service = new VotingSessionService();
            var roomId = "test-room-1";
            var connectionId = "conn-123";
            service.AddParticipant(roomId, "User1", connectionId);

            // Act
            service.RemoveParticipantByConnectionId(roomId, connectionId);
            var session = service.GetSession(roomId);

            // Assert
            Assert.Empty(session!.Participants);
        }
    }
}
