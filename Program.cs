using AttendanceSystem.Business;
using AttendanceSystem.Core.Models;
using AttendanceSystem.Data;
using AttendanceSystem.Web.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);
// Load JWT settings from appsettings.json
// Load JWT settings safely
var jwtSection = builder.Configuration.GetSection("Jwt");
JwtHelper.SecretKey = jwtSection["Key"] ?? "MySuperSecretKey123456789danishisagoodboy";
JwtHelper.Issuer = jwtSection["Issuer"] ?? "http://localhost:5013/";
JwtHelper.Audience = jwtSection["Audience"] ?? "http://localhost:5013/";

if (int.TryParse(jwtSection["ExpireMinutes"], out var expireMinutes))
{
    JwtHelper.ExpireMinutes = expireMinutes;
}
else
{
    JwtHelper.ExpireMinutes = 60; // fallback
}


// Layers
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISectionRepository, SectionRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<ISectionAssignmentRepository, SectionAssignmentRepository>();
builder.Services.AddScoped<ITimetableRepository, TimetableRepository>();
builder.Services.AddScoped<ITimetableRepository, TimetableRepository>();

// Add other repositories similarly
builder.Services.AddScoped<UserService>();
// Add this after other services


// Session with 30-minute timeout (security best practice)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// MVC
builder.Services.AddControllersWithViews();
// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

builder.Services.AddSingleton<JwtTokenService>();
// Register DbContext with connection string from appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure();
    }));
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        context.Database.EnsureCreated();
        Console.WriteLine("✅ Database connected successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database connection failed: {ex.Message}");
    }
}
using (var scope = app.Services.CreateScope())
{
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();

    // Add test user
    var result = userService.RegisterUser("test@admin.com", "Admin@123!", "Admin");
    if (result.Success)
        Console.WriteLine("Test admin user created.");
    else
        Console.WriteLine($"Error: {result.Message}");
}
// Seed ONLY the default admin
using (var scope = app.Services.CreateScope())
{
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var defaultAdminEmail = "admin@school.com";
    if (!context.Users.Any(u => u.Email == defaultAdminEmail))
    {
        // Enforce strong password for default admin
        userService.RegisterUser(defaultAdminEmail, "Admin@123!", "Admin");
    }
}

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseJwtMiddleware();
app.UseRouting();
app.UseAuthentication(); // ← Add this

app.UseSession(); // Enable session
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();