using System;
using System.Collections.Generic;

namespace PlanningPoker.Api.Models
{
    public enum VotingMode
    {
        Start,
        Voting,
        Results
    }

    public class VotingParticipant
    {
        public string Username { get; set; }
        public string? Vote { get; set; } // null if not voted yet
        public string ConnectionId { get; set; }
    }

    public class VotingSession
    {
        public string RoomId { get; set; }
        public VotingMode Mode { get; set; }
        public string? StoryName { get; set; }
        public List<string>? CardValues { get; set; } // Adjustable per vote
        public int? TimerSeconds { get; set; } // Adjustable per vote, null = no timer
        public DateTime? VotingStartTime { get; set; }
        public Dictionary<string, VotingParticipant> Participants { get; set; } = new();
        
        public bool HasUserVoted(string username)
        {
            return Participants.TryGetValue(username, out var participant) && participant.Vote != null;
        }

        public bool AllUsersVoted()
        {
            return Participants.Count > 0 && Participants.Values.All(p => p.Vote != null);
        }

        public VotingResults CalculateResults()
        {
            var results = new VotingResults
            {
                StoryName = StoryName ?? "",
                UserVotes = new Dictionary<string, string>()
            };

            var numericVotes = new List<double>();

            foreach (var participant in Participants.Values)
            {
                var vote = participant.Vote ?? "?";
                results.UserVotes[participant.Username] = vote;

                // Try to parse as numeric for average calculation
                if (double.TryParse(vote, out var numericValue))
                {
                    numericVotes.Add(numericValue);
                }
            }

            results.AverageScore = numericVotes.Count > 0 
                ? Math.Round(numericVotes.Average(), 2) 
                : (double?)null;

            return results;
        }
    }

    public class VotingResults
    {
        public string StoryName { get; set; }
        public Dictionary<string, string> UserVotes { get; set; }
        public double? AverageScore { get; set; }
    }

    // DTOs for SignalR messages
    public class StartVotingRequest
    {
        public string StoryName { get; set; }
        public List<string>? CardValues { get; set; }
        public int? TimerSeconds { get; set; }
    }

    public class SubmitVoteRequest
    {
        public string CardValue { get; set; }
    }

    public class ParticipantDto
    {
        public string Username { get; set; }
        public string? Vote { get; set; } // "?" if not voted, actual value in Results mode, null in Voting mode
        public bool HasVoted { get; set; } // true if voted in Voting mode
    }

    public class RoomStateDto
    {
        public string RoomId { get; set; }
        public string RoomName { get; set; }
        public string HostUsername { get; set; }
        public VotingMode Mode { get; set; }
        public List<ParticipantDto> Participants { get; set; }
        public string? StoryName { get; set; }
        public List<string>? CardValues { get; set; }
        public int? TimerSeconds { get; set; }
        public DateTime? VotingStartTime { get; set; }
        public VotingResults? Results { get; set; }
    }
}
