using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using iOStore.Data;
using iOStore.Helpers;
using iOStore.Models;
using iOStore.Services;
using Serilog;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog: logging a consola + archivo rotativo ─────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext());

// ── Base de datos ─────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ── Identity con confirmación de email obligatoria ────────────────────────
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;   // obliga confirmar email
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// ── Forwarded Headers (HTTPS detrás de proxy IIS en MonsterASP) ─────────
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── Data Protection Keys (persistencia entre reinicios del app pool IIS) ─
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath,
        "DataProtectionKeys")))
    .SetApplicationName("iOStore");

// ── Servicio de email SMTP real (Gmail) ───────────────────────────────────
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

// ── Servicio de códigos de verificación de 6 dígitos ─────────────────────
// Singleton: los códigos viven mientras el proceso esté activo (adecuado para hosting single-instance)
builder.Services.AddSingleton<IVerificationCodeService, VerificationCodeService>();
builder.Services.AddHostedService<VerificationCodeCleanupService>();

// ── Cookies de autenticación ──────────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// ── Caché en memoria ──────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ── MVC + Razor Pages + Sesión ────────────────────────────────────────────
builder.Services.AddControllersWithViews(options =>
{
    // Parsea decimales con InvariantCulture (punto decimal) antes de intentar
    // la cultura del servidor. Evita que "1500.00" del JS se interprete como
    // 150000 en sistemas con cultura es-AR (donde el punto es separador de miles).
    options.ModelBinderProviders.Insert(0, new iOStore.Helpers.InvariantDecimalModelBinderProvider());
});
builder.Services.AddRazorPages();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<IEnvioService, EnvioService>();
builder.Services.AddScoped<IPrecioService, PrecioService>();
builder.Services.AddSingleton<ICotizacionService, CotizacionService>();
builder.Services.AddSingleton<INotificacionService, NotificacionService>();

// ── Generación de PDF (QuestPDF) ─────────────────────────────────────────
// Licencia Community: gratuita para organizaciones con facturación < USD 1M al año.
QuestPDF.Settings.License = LicenseType.Community;
builder.Services.AddScoped<IFacturaService, FacturaService>();
builder.Services.AddSingleton<IClockService, ClockService>();

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

// ── Pipeline HTTP ─────────────────────────────────────────────────────────
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                       ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment())
    app.UseMigrationsEndPoint();
else
{
    app.UseExceptionHandler("/Home/Error/500");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSerilogRequestLogging();
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ── Seed: roles y usuario administrador ──────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    try
    {
        await SeedRolesAndAdmin(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        seedLogger.LogError(ex, "Error durante el seed inicial. Verificar cadena de conexión.");
    }
}

app.Run();

// ── Función de seed ───────────────────────────────────────────────────────
async Task SeedRolesAndAdmin(IServiceProvider sp)
{
    var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = sp.GetRequiredService<ILogger<Program>>();

    foreach (var rol in Roles.TodosLosRoles)
    {
        if (!await roleManager.RoleExistsAsync(rol))
        {
            await roleManager.CreateAsync(new IdentityRole(rol));
            logger.LogInformation("Rol '{Rol}' creado.", rol);
        }
    }

    // Admin principal
    const string adminEmail = "admin@iostore.com";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            NombreCompleto = "Administrador Principal",
            EmailConfirmed = true,
            FechaRegistro = ArClock.Now,
            FechaIncorporacion = ArClock.Now,
            Activo = true
        };
        var result = await userManager.CreateAsync(admin, "Admin123!");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, Roles.Administrador);
        else
            logger.LogError("Error creando admin: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    // Empleado de demo
    const string empEmail = "empleado@iostore.com";
    if (await userManager.FindByEmailAsync(empEmail) == null)
    {
        var emp = new ApplicationUser
        {
            UserName = empEmail,
            Email = empEmail,
            NombreCompleto = "Empleado Demo",
            EmailConfirmed = true,
            FechaRegistro = ArClock.Now,
            FechaIncorporacion = new DateTime(2024, 3, 1),
            Activo = true
        };
        var result = await userManager.CreateAsync(emp, "Empleado123!");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(emp, Roles.AdminEmpleado);
    }
}
