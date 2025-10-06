using Microsoft.AspNetCore.Mvc;
using VBlog.Models;
using VBlog.Services;
using System.Diagnostics;
using System.Linq;

namespace VBlog.Controllers
{
    public class HomeController : Controller
    {
        private readonly PostService _postService;

        public HomeController(PostService postService)
        {
            _postService = postService;
        }

        public IActionResult Index()
        {
            var posts = _postService.GetAllPosts().Where(p => p.IsPublished).ToList();
            ViewData["Title"] = "Ana Sayfa";
            return View(posts);
        }

        public IActionResult Privacy()
        {
            ViewData["Title"] = "Gizlilik Politikasý";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
