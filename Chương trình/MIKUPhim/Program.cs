using MIKUPhim.Data;
using Microsoft.EntityFrameworkCore;
using MIKUPhim.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// ===== Chatbot services =====

builder.Services.AddScoped<GeminiService>();
builder.Services.AddScoped<IntentEngine>();
builder.Services.AddScoped<MovieMatcher>();
builder.Services.AddScoped<MovieContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
/*
 Chủ nhiệm đề tài: Cao Văn Hưng
 */