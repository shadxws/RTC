using Microsoft.EntityFrameworkCore;
using RealTimeChat.Models;

namespace RealTimeChat.Data;

/// <summary>
/// Контекст базы данных приложения. 
/// Предоставляет доступ к сущностям (Chat и Message) и управляет взаимодействием с базой данных SQLite.
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Создает новый экземпляр контекста базы данных.
    /// </summary>
    /// <param name="options">Параметры конфигурации контекста, обычно предоставляются при настройке Entity Framework Core.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// DbSet для сущности Chat.
    /// Представляет набор чатов в базе данных.
    /// </summary>
    public DbSet<Chat> Chats { get; set; } = null!;

    /// <summary>
    /// DbSet для сущности Message.
    /// Представляет набор сообщений в базе данных.
    /// </summary>
    public DbSet<Message> Messages { get; set; } = null!;

    /// <summary>
    /// Настраивает модели данных и их связи с помощью ModelBuilder.
    /// Определяет структуру таблиц, первичные ключи, индексы, обязательные поля и отношения.
    /// </summary>
    /// <param name="modelBuilder">Построитель моделей, используемый для конфигурации.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Конфигурация модели Chat
        modelBuilder.Entity<Chat>(entity =>
        {
            // Устанавливаем первичный ключ
            entity.HasKey(e => e.Id);

            // Настраиваем обязательные поля и их свойства
            entity.Property(e => e.Name).IsRequired(); // Название чата обязательно
            entity.Property(e => e.EncryptionKey).IsRequired(); // Ключ шифрования обязателен
            entity.Property(e => e.EncryptionIV).IsRequired(); // Вектор инициализации обязателен

            // Устанавливаем уникальный индекс для названия чата для быстрого поиска и обеспечения уникальности
            entity.HasIndex(e => e.Name).IsUnique();

            // Настраиваем связь "один-ко-многим" с сообщениями (Chat имеет много Messages)
            entity.HasMany(e => e.Messages) // У чата есть коллекция сообщений
                .WithOne(e => e.Chat) // Каждое сообщение относится к одному чату
                .HasForeignKey(e => e.ChatId) // Внешний ключ в таблице Messages
                .OnDelete(DeleteBehavior.Cascade); // При удалении чата удаляются все связанные сообщения
        });

        // Конфигурация модели Message
        modelBuilder.Entity<Message>(entity =>
        {
            // Устанавливаем первичный ключ
            entity.HasKey(e => e.Id);

            // Настраиваем обязательные поля
            entity.Property(e => e.SenderId).IsRequired(); // Имя отправителя обязательно
            entity.Property(e => e.Content).IsRequired(); // Содержимое сообщения обязательно (зашифрованный текст)

            // Устанавливаем индекс для быстрого поиска сообщений по ChatId
            entity.HasIndex(e => e.ChatId);

            // Устанавливаем индекс для сортировки сообщений по времени
            entity.HasIndex(e => e.Timestamp);
        });
    }
} 