using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using TurnosApp.Infrastructure.Data; // Para ApplicationDbContext
using TurnosApp.Web.Seeders;        // Para DbSeeder
using TurnosApp.Core.Services;      // Para IAsignacionService
using TurnosApp.Infrastructure.Services; // Para AsignacionService
using System.Text.Json.Serialization; // Para ReferenceHandler

var builder = WebApplication.CreateBuilder(args);

// Configurar conexión a la base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Agregar servicios de la aplicación
builder.Services.AddScoped<IAsignacionService, AsignacionService>();

// Agregar controladores con vistas y configurar serialización JSON
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Habilitar sesiones
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1); // Tiempo de espera antes de que expire la sesión
    options.Cookie.HttpOnly = true;  // Solo accesible por el servidor
    options.Cookie.IsEssential = true; // Necesario para cumplir con políticas de privacidad
});

// Configurar CORS para permitir solicitudes desde el frontend (React)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")  // URL de tu frontend React
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Llamar al método DbSeeder.Seed() para poblar la base de datos
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    DbSeeder.Seed(db); // Poblar la base de datos si es necesario
}

// Configurar el pipeline de solicitudes HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Habilitar el uso de sesiones
app.UseSession();

// Habilitar CORS antes de la autorización
app.UseCors("AllowFrontend"); // 👈 Esto va aquí

app.UseAuthorization();

// Configurar rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
