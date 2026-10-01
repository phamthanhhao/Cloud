using BeeDienLanh.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// KẾT NỐI SQL SERVER
// ==========================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure();
        }));

// ==========================================
// IDENTITY + ROLE
// ==========================================
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// ==========================================
// MVC + RAZOR PAGES
// ==========================================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ==========================================
// SESSION - DÙNG CHO GIỎ HÀNG
// ==========================================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ==========================================
// XỬ LÝ LỖI
// ==========================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// ==========================================
// STATIC FILES
// ==========================================
app.UseStaticFiles();

// ==========================================
// STATIC ASSETS (.NET 10)
// ==========================================
app.MapStaticAssets();

app.UseRouting();

// ==========================================
// ĐĂNG NHẬP + PHÂN QUYỀN
// ==========================================
app.UseAuthentication();
app.UseAuthorization();

// ==========================================
// SESSION
// ==========================================
app.UseSession();

// ==========================================
// ROUTE CHO AREA
// ==========================================
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// ==========================================
// ROUTE MẶC ĐỊNH
// ==========================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

// ==========================================
// MIGRATION + TẠO ROLE + TÀI KHOẢN
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    // Áp dụng các EF Core Migration còn thiếu
    await dbContext.Database.MigrateAsync();

    // Tạo Role + Admin + Shipper
    await DbInitializer.SeedRolesAndUsersAsync(services);
}

app.Run();