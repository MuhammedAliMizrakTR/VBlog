using VBlog.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace VBlog.Services
{
    public class PostService
    {
        private readonly JsonFileService<Post> _jsonFileService;
        private readonly JsonFileService<User> _userJsonFileService;
        private readonly FileStorageService _fileStorageService;

        public PostService(JsonFileService<Post> jsonFileService, JsonFileService<User> userJsonFileService, FileStorageService fileStorageService)
        {
            _jsonFileService = jsonFileService;
            _userJsonFileService = userJsonFileService;
            _fileStorageService = fileStorageService;
        }

        private void PopulateAuthorUsernames(List<Post> posts)
        {
            if (posts == null || !posts.Any()) return;

            var userIds = posts.Select(p => p.AuthorId).Distinct().ToList();
            var users = _userJsonFileService.GetAll().Where(u => userIds.Contains(u.Id)).ToList();

            foreach (var post in posts)
            {
                var author = users.FirstOrDefault(u => u.Id == post.AuthorId);
                post.AuthorUsername = author?.Username ?? "Bilinmeyen Yazar";

                if (post.Comments != null && post.Comments.Any())
                {
                    var commentAuthorIds = post.Comments.Select(c => c.AuthorId).Distinct().ToList();
                    var commentUsers = _userJsonFileService.GetAll().Where(u => commentAuthorIds.Contains(u.Id)).ToList();

                    foreach (var comment in post.Comments)
                    {
                        var commentAuthor = commentUsers.FirstOrDefault(u => u.Id == comment.AuthorId);
                        comment.AuthorUsername = commentAuthor?.Username ?? "Bilinmeyen Kullanıcı";
                    }
                }
            }
        }

        public List<Post> GetAllPosts()
        {
            var posts = _jsonFileService.GetAll().OrderByDescending(p => p.CreatedAt).ToList();
            PopulateAuthorUsernames(posts);
            return posts;
        }

        public Post? GetPostById(Guid id)
        {
            var post = _jsonFileService.GetById(p => p.Id == id);
            if (post != null)
            {
                PopulateAuthorUsernames(new List<Post> { post });
            }
            return post;
        }

        public async Task AddPost(Post post, IFormFile? imageFile)
        {
            post.Id = Guid.NewGuid();
            post.CreatedAt = DateTime.UtcNow;

            if (imageFile != null)
            {
                try
                {
                    post.ImagePath = await _fileStorageService.SaveImageAsync(imageFile, "posts");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"Resim yüklenirken hata oluştu: {ex.Message}");
                }
            }

            _jsonFileService.Add(post);
        }

        // UpdatePost metodu güncellendi: deleteExistingImage parametresi eklendi
        public async Task UpdatePost(Post updatedPost, IFormFile? newImageFile, bool deleteExistingImage)
        {
            var allPosts = _jsonFileService.GetAll();
            var existingPost = allPosts.FirstOrDefault(p => p.Id == updatedPost.Id);

            if (existingPost != null)
            {
                // 1. Eğer "Mevcut resmi sil" seçeneği işaretliyse ve mevcut bir resim varsa, onu sil.
                if (deleteExistingImage && !string.IsNullOrEmpty(existingPost.ImagePath))
                {
                    _fileStorageService.DeleteImage(existingPost.ImagePath);
                    existingPost.ImagePath = null; // Veri modelinden de path'i temizle
                }

                // 2. Eğer yeni bir resim yükleniyorsa
                if (newImageFile != null)
                {
                    // Eğer mevcut bir resim varsa ve henüz silinmediyse (yani deleteExistingImage false ise), onu da sil.
                    // Yeni resim her zaman eskisinin yerini almalı.
                    if (!string.IsNullOrEmpty(existingPost.ImagePath))
                    {
                        _fileStorageService.DeleteImage(existingPost.ImagePath);
                    }
                    try
                    {
                        existingPost.ImagePath = await _fileStorageService.SaveImageAsync(newImageFile, "posts");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine($"Resim yüklenirken hata oluştu: {ex.Message}");
                        existingPost.ImagePath = null; // Hata durumunda resmi null yap
                    }
                }
                // Not: Eğer yeni resim yüklenmezse ve deleteExistingImage işaretlenmezse, ImagePath değişmeden kalır.

                existingPost.Title = updatedPost.Title;
                existingPost.Content = updatedPost.Content;
                existingPost.LastModified = DateTime.UtcNow;
                existingPost.IsPublished = updatedPost.IsPublished;

                _jsonFileService.SaveAll(allPosts);
            }
        }

        // DeletePost metodu güncellendi: Resimleri de silecek
        public void DeletePost(Guid id)
        {
            var postToDelete = _jsonFileService.GetById(p => p.Id == id);
            if (postToDelete != null)
            {
                if (!string.IsNullOrEmpty(postToDelete.ImagePath))
                {
                    _fileStorageService.DeleteImage(postToDelete.ImagePath); // Gönderi resmini sil
                }
                foreach (var comment in postToDelete.Comments)
                {
                    if (!string.IsNullOrEmpty(comment.ImagePath))
                    {
                        _fileStorageService.DeleteImage(comment.ImagePath); // Yorum resimlerini sil
                    }
                }
                _jsonFileService.Delete(p => p.Id == id);
            }
        }

        public async Task AddCommentToPost(Guid postId, Comment comment, IFormFile? imageFile)
        {
            var allPosts = _jsonFileService.GetAll();
            var post = allPosts.FirstOrDefault(p => p.Id == postId);

            if (post != null)
            {
                comment.Id = Guid.NewGuid();
                comment.CreatedAt = DateTime.UtcNow;

                if (imageFile != null)
                {
                    try
                    {
                        comment.ImagePath = await _fileStorageService.SaveImageAsync(imageFile, "comments");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine($"Yorum resmi yüklenirken hata oluştu: {ex.Message}");
                    }
                }

                if (post.Comments == null) { post.Comments = new List<Comment>(); }
                post.Comments.Add(comment);
                _jsonFileService.SaveAll(allPosts);
            }
        }

        public void DeleteCommentFromPost(Guid postId, Guid commentId)
        {
            var allPosts = _jsonFileService.GetAll();
            var post = allPosts.FirstOrDefault(p => p.Id == postId);

            if (post != null)
            {
                var commentToDelete = post.Comments.FirstOrDefault(c => c.Id == commentId);
                if (commentToDelete != null)
                {
                    if (!string.IsNullOrEmpty(commentToDelete.ImagePath))
                    {
                        _fileStorageService.DeleteImage(commentToDelete.ImagePath); // Yorum resmini sil
                    }
                    post.Comments.Remove(commentToDelete);
                    _jsonFileService.SaveAll(allPosts);
                }
            }
        }
    }
}