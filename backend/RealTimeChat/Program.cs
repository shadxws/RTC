using Microsoft.EntityFrameworkCore;
using RealTimeChat.Data;
using RealTimeChat.Hubs;
using RealTimeChat.Services;

// Создаем экземпляр WebApplication
var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы в контейнер зависимостей
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настраиваем политику CORS (Cross-Origin Resource Sharing)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:5022",
            "http://80.78.243.170",
            "http://80.78.243.170:80"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// Добавляем сервисы SignalR для работы в реальном времени
builder.Services.AddSignalR();

// Настраиваем контекст базы данных с использованием SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))); // Получаем строку подключения из конфигурации

// Регистрируем сервис шифрования как Scoped (создается один раз для каждого запроса/соединения)
builder.Services.AddScoped<EncryptionService>();

// Собираем экземпляр приложения
var app = builder.Build();

// Конфигурируем конвейер обработки HTTP запросов.
// В среде разработки включаем Swagger для документации API.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Убрали HTTPS редирект, используем только HTTP
// Используем настроенную политику CORS
app.UseCors();
// Включаем авторизацию (если используется)
app.UseAuthorization();

// Настраиваем маршрутизацию контроллеров
app.MapControllers();
// Настраиваем маршрут для SignalR хаба
app.MapHub<ChatHub>("/chatHub");

// Создаем базу данных при запуске приложения, если она еще не существует
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // EnsureCreated() создает базу данных и таблицы, если их нет. 
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        // Логируем ошибку, если что-то пошло не так при создании базы данных
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Произошла ошибка при создании базы данных");
    }
}

// Запускаем приложение
app.Run();