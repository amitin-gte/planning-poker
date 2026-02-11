using PlanningPoker.Api.Models;
using System.Collections.Concurrent;

namespace PlanningPoker.Api.Services
{
    public class TokenService
    {
        private readonly ConcurrentDictionary<string, User> _tokens;

        public TokenService()
        {
            _tokens = new ConcurrentDictionary<string, User>();
        }

        public string GenerateToken(User user)
        {
            var token = Guid.NewGuid().ToString();
            _tokens[token] = user;
            return token;
        }

        public User ValidateToken(string token)
        {
            if (_tokens.TryGetValue(token, out var user))
            {
                return user;
            }
            return null;
        }

        public bool RevokeToken(string token)
        {
            return _tokens.TryRemove(token, out _);
        }

        public void Clear()
        {
            _tokens.Clear();
        }
    }
}
