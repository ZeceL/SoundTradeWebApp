using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SoundTradeWebApp.Data; // ���� ������������ ���� ��� DbContext

var builder = WebApplication.CreateBuilder(args);

// --- ����������� �������� ---

// 1. ���������� ��������� MVC (����������� � �������������)
builder.Services.AddControllersWithViews();

// 2. ����������� DbContext ��� ������ � SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. ��������� Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";       // �������� ����� (���� ������ ��������)
        options.AccessDeniedPath = "/Account/AccessDenied"; // �������� ������ ������� (���� ���� �� ��������)
        options.ExpireTimeSpan = TimeSpan.FromDays(30); // ����� ����� ���� (30 ����)
        options.SlidingExpiration = true;           // ���������� ����� ����� ��� ����������
        options.Cookie.HttpOnly = true;             // ���� �������� ������ ������� (������ �� XSS)
        options.Cookie.IsEssential = true;          // �������� ���� ��� ����������� (��� GDPR)
        // options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // ������ HTTPS (����������������� ��� ������������� �� HTTPS)
    });

// 4. ���������� �������� �����������
builder.Services.AddAuthorization();

// --- ������ ���������� ---
var app = builder.Build();

// --- ��������� Pipeline (Middleware) ---

// 5. ��������� ������ (� ������ ���������� ���������� ��������� ��������)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // �������� ��� ����������� ������ ������������
    app.UseHsts(); // �������������� ������������� HTTPS (����� ������� ��������� ����������)
}
else
{
    app.UseDeveloperExceptionPage(); // �������� � �������� ������ ��� ������������
}

// 6. ��������������� �� HTTPS
app.UseHttpsRedirection();

// 7. ��������� ����������� ������ (CSS, JS, ����������� �� wwwroot)
app.UseStaticFiles();

// 8. ������������� ��������
app.UseRouting();

// 9. ��������� �������������� (�����: ����� UseAuthorization)
app.UseAuthentication();

// 10. ��������� �����������
app.UseAuthorization();

// 11. ��������� ��������� �� ��������� (Controller=Home, Action=Index, ������������ id)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// --- ������ ���������� ---
app.Run();