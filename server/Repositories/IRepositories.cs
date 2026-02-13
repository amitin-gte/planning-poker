using PlanningPoker.Api.Models;

namespace PlanningPoker.Api.Repositories
{
    public interface IRoomRepository
    {
        RoomConfig Create(RoomConfig room);
        bool Update(RoomConfig room);
        RoomConfig? Get(string roomId);
        bool Delete(string roomId);
        List<RoomConfig> GetAll();
    }

    public interface IUserRepository
    {
        User? Create(string username, string password, UserRole role);
        User? SignIn(string username, string password);
        bool Delete(string username);
        List<User> List();
        int Count();
    }
}
