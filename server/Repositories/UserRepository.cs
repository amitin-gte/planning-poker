using LiteDB;
using PlanningPoker.Api.Models;
using System.Collections.Generic;
using System.Linq;

namespace PlanningPoker.Api.Repositories
{
    public class UserRepository : IDisposable
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<User> _users;
        private bool _disposed = false;

        public UserRepository(string dbPath = "Users.db")
        {
            _db = new LiteDatabase(dbPath);
            _users = _db.GetCollection<User>("users");
            _users.EnsureIndex(x => x.Username);
        }

        public User Create(string username, string password, UserRole role)
        {
            var existingUser = _users.FindOne(x => x.Username == username);
            if (existingUser != null)
            {
                return null; // User already exists
            }

            var user = new User
            {
                Username = username,
                Password = password,
                Role = role
            };

            _users.Insert(user);
            return user;
        }

        public User SignIn(string username, string password)
        {
            var user = _users.FindOne(x => x.Username == username);
            if (user != null && user.Password == password)
            {
                return user;
            }
            return null; // User not found or password mismatch
        }

        public List<User> List()
        {
            return _users.FindAll().ToList();
        }

        public bool Delete(string username)
        {
            return _users.DeleteMany(x => x.Username == username) > 0;
        }

        public int Count()
        {
            return _users.Count();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _db?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
