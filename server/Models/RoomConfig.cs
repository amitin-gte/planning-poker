using System.Collections.Generic;

namespace PlanningPoker.Api.Models
{
    public class RoomConfig
    {
        [LiteDB.BsonId]
        public string RoomId { get; set; } // Generated unique id
        public string Name { get; set; } // Room name
        public string HostUsername { get; set; } // Username of the host who created the room
        public List<string> PokerCards { get; set; } // Custom poker cards
        public int VotingCountdownSeconds { get; set; } // Countdown timer in seconds
    }
}
