using Microsoft.AspNetCore.Authentication.Cookies;
using VBlog.Models;
using VBlog.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// --- 1. JSON Veri Dosyalar�n� Tan�mlama ve DI Yap�land�rmas� ---
var dataFolderPath = Path.Combine(builder.Environment.ContentRootPath, "Data");
Directory.CreateDirectory(dataFolderPath);

var usersFilePath = Path.Combine(dataFolderPath, "users.json");
var postsFilePath = Path.Combine(dataFolderPath, "posts.json");

builder.Services.AddSingleton(new JsonFileService<User>(usersFilePath));
builder.Services.AddSingleton(new JsonFileService<Post>(postsFilePath));

// Yeni: FileStorageService'i kaydet
builder.Services.AddSingleton<FileStorageService>(); // YEN� EKLEND�

builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<PostService>();

// --- 2. Kimlik Do�rulama ve Yetkilendirme (Cookie Tabanl�) ---
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
    options.AddPolicy("AuthorOrAdmin", policy => policy.RequireRole("Admin", "Author")); // BU POL�T�KA KULLANILACAK
    options.AddPolicy("RegisteredUser", policy => policy.RequireAuthenticatedUser());
});



// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// --- 3. Ba�lang�� Verilerini (Seed Data) Olu�turma ---
using (var scope = app.Services.CreateScope())
{
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
    var postService = scope.ServiceProvider.GetRequiredService<PostService>();
    // Seed data olu�tururken resim eklemek isterseniz, burada FileStorageService'i de kullanmal�s�n�z.
    // �imdilik seed data i�in resim y�klemeyi atlayabiliriz, elle y�kleyece�iz.

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

        // Seed data'ya resim eklenecekse, bu k�s�m FileStorageService kullan�larak g�ncellenmelidir.
        // �imdilik sadece text post olarak kals�n.
        if (adminUser != null)
        {
            await postService.AddPost(new Post
            {
                Title = "VBlog'a Ho� Geldiniz!",
                Content = "Bu ilk g�nderimiz. Blog sitemiz ASP.NET Core MVC, JSON dosyalar� ve BCrypt ile g��lendirilmi�tir.",
                AuthorId = adminUser.Id,
                IsPublished = true
            }, null); // Resim yok
        }
        if (authorUser != null)
        {
            await postService.AddPost(new Post
            {
                Title = "Yazar�n �lk G�nderisi",
                Content = "Merhaba d�nya! Ben yeni yazar�n�z.",
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