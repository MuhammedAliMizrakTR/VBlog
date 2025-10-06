using VBlog.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using BCrypt.Net;

namespace VBlog.Services
{
    public class UserService
    {
        private readonly JsonFileService<User> _jsonFileService;

        public UserService(JsonFileService<User> jsonFileService)
        {
            _jsonFileService = jsonFileService;
        }

        public User? GetUserById(Guid id) => _jsonFileService.GetById(u => u.Id == id);
        public User? GetUserByUsername(string username) => _jsonFileService.GetById(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        public User? GetUserByEmail(string email) => _jsonFileService.GetById(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        public List<User> GetAllUsers() => _jsonFileService.GetAll();

        public User? RegisterUser(RegisterViewModel model, string role = "User")
        {
            if (GetUserByUsername(model.Username) != null || GetUserByEmail(model.Email) != null)
            {
                return null; 
            }

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Username = model.Username,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password), 
                Role = role,
                RegisteredAt = DateTime.UtcNow
            };
            _jsonFileService.Add(newUser);
            return newUser;
        }

        public User? ValidateUser(string username, string password)
        {
            var user = GetUserByUsername(username);
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) 
            {
                return user;
            }
            return null;
        }

        public void UpdateUser(User user)
        {
            var allUsers = _jsonFileService.GetAll();
            var existingUser = allUsers.FirstOrDefault(u => u.Id == user.Id);
            if (existingUser != null)
            {
                existingUser.Username = user.Username;
                existingUser.Email = user.Email;
                
                if (!string.IsNullOrEmpty(user.PasswordHash) && user.PasswordHash.Length > 20) 
                {
                    existingUser.PasswordHash = user.PasswordHash;
                }
                existingUser.Role = user.Role;
                _jsonFileService.SaveAll(allUsers);
            }
        }

        public void DeleteUser(Guid id)
        {
            _jsonFileService.Delete(u => u.Id == id);
        }
    }
}