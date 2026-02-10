using LiteDB;
using PlanningPoker.Api.Models;
using PlanningPoker.Api.Repositories;
using System.Collections.Generic;
using Xunit;

namespace PlanningPoker.Tests
{
    public class RoomRepositoryTests
    {
        private RoomRepository GetRepo()
        {
            // Use in-memory LiteDB for tests
            return new RoomRepository("Filename=:memory:");
        }

        [Fact]
        public void CreateAndGetRoom_Works()
        {
            var repo = GetRepo();
            var room = new RoomConfig { Name = "Test Room", PokerCards = new List<string> { "1", "2", "3" }, VotingCountdownSeconds = 30 };
            var created = repo.Create(room);
            var fetched = repo.Get(created.RoomId);
            Assert.NotNull(fetched);
            Assert.Equal("Test Room", fetched.Name);
            Assert.Equal(30, fetched.VotingCountdownSeconds);
        }

        [Fact]
        public void UpdateRoom_Works()
        {
            var repo = GetRepo();
            var room = repo.Create(new RoomConfig { Name = "Room", PokerCards = new List<string> { "A" }, VotingCountdownSeconds = 10 });
            room.Name = "Updated Room";
            var updated = repo.Update(room);
            Assert.True(updated);
            var fetched = repo.Get(room.RoomId);
            Assert.Equal("Updated Room", fetched.Name);
        }

        [Fact]
        public void DeleteRoom_Works()
        {
            var repo = GetRepo();
            var room = repo.Create(new RoomConfig { Name = "DeleteMe", PokerCards = new List<string> { "X" }, VotingCountdownSeconds = 5 });
            var deleted = repo.Delete(room.RoomId);
            Assert.True(deleted);
            Assert.Null(repo.Get(room.RoomId));
        }

        [Fact]
        public void GetAllRooms_Works()
        {
            var repo = GetRepo();
            repo.Create(new RoomConfig { Name = "Room1", PokerCards = new List<string> { "1" }, VotingCountdownSeconds = 10 });
            repo.Create(new RoomConfig { Name = "Room2", PokerCards = new List<string> { "2" }, VotingCountdownSeconds = 20 });
            var all = repo.GetAll();
            Assert.Equal(2, all.Count);
        }
    }
}
