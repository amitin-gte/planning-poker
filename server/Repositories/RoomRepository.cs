using LiteDB;
using PlanningPoker.Api.Models;
using System.Collections.Generic;
using System.Linq;

namespace PlanningPoker.Api.Repositories
{
    public class RoomRepository : IDisposable
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<RoomConfig> _rooms;
        private bool _disposed = false;

        public RoomRepository(string dbPath = "Rooms.db")
        {
            _db = new LiteDatabase(dbPath);
            _rooms = _db.GetCollection<RoomConfig>("rooms");
            _rooms.EnsureIndex(x => x.RoomId);
        }

        public RoomConfig Create(RoomConfig room)
        {
            room.RoomId = ObjectId.NewObjectId().ToString();
            _rooms.Insert(room);
            return room;
        }

        public bool Update(RoomConfig room)
        {
            return _rooms.Update(room);
        }

        public RoomConfig Get(string roomId)
        {
            return _rooms.FindOne(x => x.RoomId == roomId);
        }

        public bool Delete(string roomId)
        {
            return _rooms.DeleteMany(x => x.RoomId == roomId) > 0;
        }

        public List<RoomConfig> GetAll()
        {
            return _rooms.FindAll().ToList();
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
