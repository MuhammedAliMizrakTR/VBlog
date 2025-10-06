using Microsoft.AspNetCore.Authentication.Cookies;
using VBlog.Models;
using VBlog.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// --- 1. JSON Veri Dosyalarýný Tanýmlama ve DI Yapýlandýrmasý ---
var dataFolderPath = Path.Combine(builder.Environment.ContentRootPath, "Data");
Directory.CreateDirectory(dataFolderPath);

var usersFilePath = Path.Combine(dataFolderPath, "users.json");
var postsFilePath = Path.Combine(dataFolderPath, "posts.json");

builder.Services.AddSingleton(new JsonFileService<User>(usersFilePath));
builder.Services.AddSingleton(new JsonFileService<Post>(postsFilePath));

// Yeni: FileStorageService'i kaydet
builder.Services.AddSingleton<FileStorageService>(); // YENÝ EKLENDÝ

builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<PostService>();

// --- 2. Kimlik Doðrulama ve Yetkilendirme (Cookie Tabanlý) ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });



builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AuthorOrAdmin", policy => policy.RequireRole("Admin", "Author")); // BU POLÝTÝKA KULLANILACAK
    options.AddPolicy("RegisteredUser", policy => policy.RequireAuthenticatedUser());
});



// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// --- 3. Baþlangýç Verilerini (Seed Data) Oluþturma ---
using (var scope = app.Services.CreateScope())
{
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
    var postService = scope.ServiceProvider.GetRequiredService<PostService>();
    // Seed data oluþtururken resim eklemek isterseniz, burada FileStorageService'i de kullanmalýsýnýz.
    // Þimdilik seed data için resim yüklemeyi atlayabiliriz, elle yükleyeceðiz.

    if (!userService.GetAllUsers().Any())
    {
        var adminUser = userService.RegisterUser(new RegisterViewModel
        {
            Username = "admin",
            Email = "admin@vblog.com",
            Password = "AdminPassword123",
            ConfirmPassword = "AdminPassword123"
        }, "Admin");

        var authorUser = userService.RegisterUser(new RegisterViewModel
        {
            Username = "author",
            Email = "author@vblog.com",
            Password = "AuthorPassword123",
            ConfirmPassword = "AuthorPassword123"
        }, "Author");

        // Seed data'ya resim eklenecekse, bu kýsým FileStorageService kullanýlarak güncellenmelidir.
        // Þimdilik sadece text post olarak kalsýn.
        if (adminUser != null)
        {
            await postService.AddPost(new Post
            {
                Title = "VBlog'a Hoþ Geldiniz!",
                Content = "Bu ilk gönderimiz. Blog sitemiz ASP.NET Core MVC, JSON dosyalarý ve BCrypt ile güçlendirilmiþtir.",
                AuthorId = adminUser.Id,
                IsPublished = true
            }, null); // Resim yok
        }
        if (authorUser != null)
        {
            await postService.AddPost(new Post
            {
                Title = "Yazarýn Ýlk Gönderisi",
                Content = "Merhaba dünya! Ben yeni yazarýnýz.",
                AuthorId = authorUser.Id,
                IsPublished = true
            }, null); // Resim yok
        }
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();