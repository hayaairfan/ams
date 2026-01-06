using AttendanceSystem.Core.Models;
using System.Collections.Generic;

namespace AttendanceSystem.Data
{
    public interface IUserRepository
    {
        User? GetUserByEmail(string email);
        void AddUser(User user);
        bool UserExists(string email);
        List<User> GetAllUsers();
        void UpdateUser(User user);
    }
}