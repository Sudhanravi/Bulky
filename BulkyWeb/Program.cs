
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Bulky.Utilities;
using Stripe;
using Bulky.DataAccess.DBInitializer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//configuring dbContext (new)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
  options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConection"))
);

//for assigning appsettings.json stripe keys to utilities (new)
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

//this is for identity it will add by default when u r scaffolding identity (new)
builder.Services.AddIdentity<IdentityUser, IdentityRole>(/*options => options.SignIn.RequireConfirmedAccount = true*/).AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

//Adding session (new)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(100);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//adding identity route path (new)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});

//facebook login configuration
builder.Services.AddAuthentication().AddFacebook(options =>
{
    options.AppId = "1500700890590213";
    options.AppSecret = "a2cefc9365f67b7520ecdd242116ef5d";
});

//since we are using identity those all are razor pages. we need to add this service in order to do that. (new)
builder.Services.AddRazorPages();

//registering repositories (new)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

//for email (new)
builder.Services.AddScoped<IEmailSender, EmailSender>();

//for initializing DBInitializer (adding admin user creating roles) (new)
builder.Services.AddScoped<IDBInitializer, DBInitializer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

//configuring stripe (new)
StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<String>();

app.UseRouting();

//adding session (new)
app.UseSession();

//for initializing DBInitializer (adding admin user creating roles) (new)
SeedDatabase();

//must add here after scaffolding identity (new)
app.UseAuthentication();

app.UseAuthorization();

//we have to add this also along with razor service above (new)
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();

//for initializing DBInitializer (adding admin user creating roles) (new)
void SeedDatabase()
{
    using (var scope = app.Services.CreateScope())
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDBInitializer>();
        dbInitializer.Initialize();
    }
}
