using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; 

namespace VBlog.Models
{
    public class Comment
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public Guid AuthorId { get; set; }
        public string AuthorUsername { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yorum içeriği boş bırakılamaz.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Yorum 5 ila 500 karakter arasında olmalıdır.")]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? ImagePath { get; set; } 
    }

    public class CommentViewModel
    {
        public Guid PostId { get; set; }

        [Required(ErrorMessage = "Yorum içeriği boş bırakılamaz.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Yorum 5 ila 500 karakter arasında olmalıdır.")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Resim Yükle")]
        [DataType(DataType.Upload)]
        public IFormFile? ImageFile { get; set; } 
    }
}