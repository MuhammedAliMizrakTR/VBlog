using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VBlog.Models;
using VBlog.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace VBlog.Controllers
{
    public class PostController : Controller
    {
        private readonly PostService _postService;
        private readonly UserService _userService;

        public PostController(PostService postService, UserService userService)
        {
            _postService = postService;
            _userService = userService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Details(Guid id)
        {
            var post = _postService.GetPostById(id);
            if (post == null)
            {
                return NotFound();
            }

            if (!post.IsPublished)
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!User.IsInRole("Admin") && currentUserId != post.AuthorId.ToString())
                {
                    return NotFound();
                }
            }
            ViewData["Title"] = post.Title;
            return View(post);
        }

        // BURASI DEĞİŞTİ: Artık sadece Author veya Admin rolündekiler gönderi oluşturabilir.
        [Authorize(Policy = "AuthorOrAdmin")]
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Yeni Gönderi Oluştur";
            return View();
        }

        // BURASI DEĞİŞTİ: Artık sadece Author veya Admin rolündekiler gönderi oluşturabilir.
        [Authorize(Policy = "AuthorOrAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PostViewModel model)
        {
            if (ModelState.IsValid)
            {
                var authorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (authorIdClaim == null)
                {
                    return Unauthorized();
                }
                var authorId = Guid.Parse(authorIdClaim);

                var newPost = new Post
                {
                    Title = model.Title,
                    Content = model.Content,
                    AuthorId = authorId,
                    IsPublished = true
                };

                try
                {
                    await _postService.AddPost(newPost, model.ImageFile);
                    TempData["SuccessMessage"] = "Gönderiniz başarıyla oluşturuldu!";
                    return RedirectToAction("Details", new { id = newPost.Id });
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }
            ViewData["Title"] = "Yeni Gönderi Oluştur";
            return View(model);
        }

        [Authorize(Policy = "AuthorOrAdmin")]
        [HttpGet]
        public IActionResult Edit(Guid id)
        {
            var post = _postService.GetPostById(id);
            if (post == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && currentUserId != post.AuthorId.ToString())
            {
                return Forbid();
            }

            var viewModel = new PostViewModel
            {
                Title = post.Title,
                Content = post.Content,
                ExistingImagePath = post.ImagePath
            };
            ViewData["Title"] = $"'{post.Title}' Düzenle";
            return View(viewModel);
        }

        [Authorize(Policy = "AuthorOrAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, PostViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingPost = _postService.GetPostById(id);
                if (existingPost == null)
                {
                    return NotFound();
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!User.IsInRole("Admin") && currentUserId != existingPost.AuthorId.ToString())
                {
                    return Forbid();
                }

                existingPost.Title = model.Title;
                existingPost.Content = model.Content;

                try
                {
                    await _postService.UpdatePost(existingPost, model.ImageFile, model.DeleteExistingImage);
                    TempData["SuccessMessage"] = "Gönderi başarıyla güncellendi.";
                    return RedirectToAction("Details", new { id = existingPost.Id });
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    var originalPost = _postService.GetPostById(id);
                    if (originalPost != null)
                    {
                        model.ExistingImagePath = originalPost.ImagePath;
                    }
                }
            }
            var originalPostForTitle = _postService.GetPostById(id); // Hata durumunda başlık için
            ViewData["Title"] = $"'{originalPostForTitle?.Title ?? "Gönderi"}' Düzenle";
            return View(model);
        }

        [Authorize(Policy = "AuthorOrAdmin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(Guid id)
        {
            var post = _postService.GetPostById(id);
            if (post == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && currentUserId != post.AuthorId.ToString())
            {
                return Forbid();
            }

            _postService.DeletePost(id);
            TempData["SuccessMessage"] = "Gönderi başarıyla silindi.";
            return RedirectToAction("Index", "Home");
        }

        // Yorum yapmak için hala RegisteredUser yetkisi yeterli (giriş yapmış olmak)
        [Authorize(Policy = "RegisteredUser")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(CommentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var authorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var authorUsername = User.FindFirstValue(ClaimTypes.Name);
                if (authorIdClaim == null || authorUsername == null)
                {
                    return Unauthorized();
                }
                var authorId = Guid.Parse(authorIdClaim);

                var comment = new Comment
                {
                    PostId = model.PostId,
                    AuthorId = authorId,
                    AuthorUsername = authorUsername,
                    Content = model.Content
                };
                try
                {
                    await _postService.AddCommentToPost(model.PostId, comment, model.ImageFile);
                    TempData["SuccessMessage"] = "Yorumunuz başarıyla eklendi.";
                    return RedirectToAction("Details", new { id = model.PostId });
                }
                catch (InvalidOperationException ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                    return RedirectToAction("Details", new { id = model.PostId });
                }
            }
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage)
                                        .ToList();
            TempData["ErrorMessage"] = "Yorum eklenirken bazı hatalar oluştu: " + string.Join(" ", errors);
            return RedirectToAction("Details", new { id = model.PostId });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteComment(Guid postId, Guid commentId)
        {
            _postService.DeleteCommentFromPost(postId, commentId);
            TempData["SuccessMessage"] = "Yorum başarıyla silindi.";
            return RedirectToAction("Details", new { id = postId });
        }
    }
}