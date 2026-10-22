using Microsoft.EntityFrameworkCore;
using PhoneShop.Data;
using Microsoft.AspNetCore.Identity;
using PhoneShop.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
if (app.Environment.IsDevelopment())
{
    var adminEmail = app.Configuration["AdminSetup:Email"];

    if (!string.IsNullOrWhiteSpace(adminEmail))
    {
        using var scope = app.Services.CreateScope();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var adminUser = await userManager.FindByEmailAsync(
            adminEmail.Trim());

        if (adminUser == null)
        {
            throw new InvalidOperationException(
                "Email AdminSetup chưa được đăng ký trong PhoneShop.");
        }

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var createRole = await roleManager.CreateAsync(
                new IdentityRole("Admin"));

            if (!createRole.Succeeded)
            {
                throw new InvalidOperationException(
                    "Không tạo được quyền Admin.");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var assignRole = await userManager.AddToRoleAsync(
                adminUser, "Admin");

            if (!assignRole.Succeeded)
            {
                throw new InvalidOperationException(
                    "Không cấp được quyền Admin.");
            }
        }
    }
}

app.Run();
