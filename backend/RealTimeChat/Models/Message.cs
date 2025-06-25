namespace RealTimeChat.Models;

/// <summary>
/// Модель данных, представляющая сообщение в чате.
/// Хранит содержимое сообщения, информацию об отправителе и времени отправки.
/// </summary>
public class Message
{
    /// <summary>
    /// Уникальный идентификатор сообщения. Является первичным ключом в базе данных.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор чата, к которому относится сообщение.
    /// Является внешним ключом для связи с таблицей Chats.
    /// </summary>
    public int ChatId { get; set; }

    /// <summary>
    /// Идентификатор отправителя сообщения (например, имя пользователя).
    /// </summary>
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// Зашифрованное содержимое сообщения.
    /// Хранится в формате Base64.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Дата и время отправки сообщения.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Навигационное свойство Entity Framework Core для связи с чатом.
    /// </summary>
    public Chat? Chat { get; set; }
} 