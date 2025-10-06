using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VBlog.Models;
using VBlog.Services;
using System;
using System.Linq;
using System.Collections.Generic;
using BCrypt.Net;
using System.Threading.Tasks;

namespace VBlog.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private readonly UserService _userService;
        private readonly PostService _postService;

        public AdminController(UserService userService, PostService postService)
        {
            _userService = userService;
            _postService = postService;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Admin Paneli";
            return View();
        }

        // --- Kullanıcı Yönetimi ---
        public IActionResult ManageUsers()
        {
            var users = _userService.GetAllUsers();
            ViewData["Title"] = "Kullanıcıları Yönet";
            return View(users);
        }

        [HttpGet]
        public IActionResult EditUser(Guid id)
        {
            var user = _userService.GetUserById(id);
            if (user == null) return NotFound();
            ViewData["Title"] = $"'{user.Username}' Kullanıcısını Düzenle";
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(User user, string? newPassword)
        {
            var existingUser = _userService.GetUserById(user.Id);
            if (existingUser == null) return NotFound();

            // Sadece kullanıcı adı veya e-posta değiştiyse benzersizlik kontrolü yap
            if (!existingUser.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase) && _userService.GetUserByUsername(user.Username) != null)
            {
                ModelState.AddModelError("Username", "Bu kullanıcı adı zaten kullanımda.");
            }
            if (!existingUser.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase) && _userService.GetUserByEmail(user.Email) != null)
            {
                ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanımda.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = $"'{user.Username}' Kullanıcısını Düzenle";
                return View(user);
            }

            existingUser.Username = user.Username;
            existingUser.Email = user.Email;
            existingUser.Role = user.Role;

            if (!string.IsNullOrEmpty(newPassword))
            {
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            }

            _userService.UpdateUser(existingUser);
            TempData["SuccessMessage"] = "Kullanıcı başarıyla güncellendi.";
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(Guid id)
        {
            _userService.DeleteUser(id);
            TempData["SuccessMessage"] = "Kullanıcı başarıyla silindi.";
            return RedirectToAction("ManageUsers");
        }

        // YENİ EKLENDİ: Admin panelinden kullanıcı oluşturma (GET)
        [HttpGet]
        public IActionResult CreateUser()
        {
            ViewData["Title"] = "Yeni Kullanıcı Oluştur";
            return View();
        }

        // YENİ EKLENDİ: Admin panelinden kullanıcı oluşturma (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // CreateUserViewModel, UserService'in RegisterUser metoduyla uyumlu olmadığı için
                // RegisterViewModel'e dönüştürerek veya UserService'e yeni bir metot ekleyerek kullanabiliriz.
                // Basitlik adına RegisterViewModel'e dönüştürelim.
                var registerModel = new RegisterViewModel
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = model.Password,
                    ConfirmPassword = model.ConfirmPassword
                };

                var newUser = _userService.RegisterUser(registerModel, model.Role);
                if (newUser == null)
                {
                    ModelState.AddModelError(string.Empty, "Kullanıcı adı veya e-posta zaten kullanımda.");
                    ViewData["Title"] = "Yeni Kullanıcı Oluştur";
                    return View(model);
                }
                TempData["SuccessMessage"] = $"'{newUser.Username}' adlı kullanıcı başarıyla oluşturuldu.";
                return RedirectToAction("ManageUsers");
            }
            ViewData["Title"] = "Yeni Kullanıcı Oluştur";
            return View(model);
        }


        // --- Gönderi Yönetimi ---
        public IActionResult ManagePosts()
        {
            var posts = _postService.GetAllPosts();
            ViewData["Title"] = "Gönderileri Yönet";
            return View(posts);
        }

        [HttpGet]
        public IActionResult EditPost(Guid id)
        {
            var post = _postService.GetPostById(id);
            if (post == null) return NotFound();
            ViewData["Title"] = $"'{post.Title}' Gönderisini Düzenle";
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(Post post)
        {
            if (ModelState.IsValid)
            {
                await _postService.UpdatePost(post, null, false);

                TempData["SuccessMessage"] = "Gönderi başarıyla güncellendi.";
                return RedirectToAction("ManagePosts");
            }
            ViewData["Title"] = $"'{post.Title}' Gönderisini Düzenle";
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(Guid id)
        {
            _postService.DeletePost(id);
            TempData["SuccessMessage"] = "Gönderi başarıyla silindi.";
            return RedirectToAction("ManagePosts");
        }

        // --- Yorum Yönetimi (Genel) ---
        public IActionResult ManageComments()
        {
            var allPosts = _postService.GetAllPosts();
            var allComments = new List<Comment>();

            foreach (var post in allPosts)
            {
                foreach (var comment in post.Comments)
                {
                    comment.PostId = post.Id;
                    allComments.Add(comment);
                }
            }
            ViewData["Title"] = "Yorumları Yönet";
            return View(allComments.OrderByDescending(c => c.CreatedAt).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteComment(Guid postId, Guid commentId)
        {
            _postService.DeleteCommentFromPost(postId, commentId);
            TempData["SuccessMessage"] = "Yorum başarıyla silindi.";
            return RedirectToAction("ManageComments");
        }
    }
}