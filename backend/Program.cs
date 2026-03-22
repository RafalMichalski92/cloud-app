using CloudBackend.Data;
using Microsoft.EntityFrameworkCore;
using CloudBackend.Models;

var builder = WebApplication.CreateBuilder(args);

// --- 1. KONFIGURACJA POŁĄCZENIA ---
// Pobieramy connection string (zmiennej środowiskowej z Dockera/Azure lub appsettings.json)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// --- 2. SEKCJA USŁUG (Dependency Injection) ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Rejestracja bazy danych z mechanizmem ponawiania prób (Retry Logic) - KROK Z TWOJEGO ZDJĘCIA
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => 
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,                          // Ile razy ma próbować
            maxRetryDelay: TimeSpan.FromSeconds(30),  // Maksymalny odstęp między próbami
            errorNumbersToAdd: null)                  // Dodatkowe kody błędów (opcjonalnie)
    ));

// Konfiguracja CORS - żeby Frontend mógł rozmawiać z Backendem
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// --- 3. AUTOMATYCZNE TWORZENIE BAZY ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // EnsureCreated stworzy tabele, jeśli baza danych już istnieje, ale jest pusta
        context.Database.EnsureCreated();
        
        // Seedowanie danych (opcjonalne, na start)
        if (!context.Tasks.Any())
        {
            context.Tasks.AddRange(
                new CloudTask { Name = "Zrobić kawę", IsCompleted = true },
                new CloudTask { Name = "Zrobić zadanie z Azure", IsCompleted = false }
            );
            context.SaveChanges();
            Console.WriteLine("Baza danych gotowa!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Błąd podczas inicjalizacji bazy: {ex.Message}");
    }
}

// --- 4. MIDDLEWARE (Kolejność ma znaczenie!) ---
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cloud API V1");
    c.RoutePrefix = string.Empty; // Swagger na głównym adresie backendu
});

app.UseCors();
app.MapControllers();

app.Run();