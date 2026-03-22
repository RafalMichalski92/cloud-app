using CloudBackend.Data;
using Microsoft.EntityFrameworkCore;
using CloudBackend.Models;

var builder = WebApplication.CreateBuilder(args); // 1. NAJPIERW tworzymy buildera

// --- KONFIGURACJA POŁĄCZENIA ---
// .NET sam priorytetyzuje zmienne środowiskowe z Dockera nad appsettings.json,
// więc wystarczy jedna czysta linijka:
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// --- SEKCJA USŁUG (Dependency Injection) ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build(); // 2. BUDUJEMY aplikację

// --- AUTOMATYCZNE TWORZENIE BAZY (Odkoduj to, jeśli chcesz autostart bazy) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
        if (!context.Tasks.Any())
        {
            context.Tasks.AddRange(
                new CloudTask { Name = "Zrobić kawę", IsCompleted = true },
                new CloudTask { Name = "Uruchomić projekt w Dockerze", IsCompleted = false }
            );
            context.SaveChanges();
            Console.WriteLine("Baza danych zainicjalizowana pomyślnie!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Błąd bazy danych: {ex.Message}");
    }
}

// --- MIDDLEWARE ---
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cloud API V1");
    c.RoutePrefix = string.Empty; // Swagger będzie dostępny od razu pod localhost:8081
});

app.UseCors();
app.MapControllers();

app.Run();