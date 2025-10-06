using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // IFormFile için

namespace VBlog.Models
{
    public class Post
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Başlık boş bırakılamaz.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Başlık 5 ila 200 karakter arasında olmalıdır.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "İçerik boş bırakılamaz.")]
        [MinLength(20, ErrorMessage = "İçerik en az 20 karakter uzunluğunda olmalıdır.")]
        public string Content { get; set; } = string.Empty;

        public Guid AuthorId { get; set; }
        public string AuthorUsername { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModified { get; set; }
        public bool IsPublished { get; set; } = true;

        public string? ImagePath { get; set; }

        public List<Comment> Comments { get; set; } = new List<Comment>();
    }

    public class PostViewModel
    {
        [Required(ErrorMessage = "Başlık boş bırakılamaz.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Başlık 5 ila 200 karakter arasında olmalıdır.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "İçerik boş bırakılamaz.")]
        [MinLength(20, ErrorMessage = "İçerik en az 20 karakter uzunluğunda olmalıdır.")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Resim Yükle")]
        [DataType(DataType.Upload)]
        public IFormFile? ImageFile { get; set; }

        public string? ExistingImagePath { get; set; }

        public bool DeleteExistingImage { get; set; } // YENİ EKLENDİ: Mevcut resmi silmek için checkbox durumu
    }
}