namespace RealTimeChat.Models;

/// <summary>
/// Модель данных, представляющая чат в приложении.
/// Хранит информацию о чате, включая название, время создания и ключи шифрования.
/// </summary>
public class Chat
{
    /// <summary>
    /// Уникальный идентификатор чата. Является первичным ключом в базе данных.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Название чата. Должно быть уникальным.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Дата и время создания чата.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Ключ шифрования AES для сообщений в этом чате.
    /// Хранится в формате Base64.
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>
    /// Вектор инициализации AES для сообщений в этом чате.
    /// Хранится в формате Base64.
    /// </summary>
    public string EncryptionIV { get; set; } = string.Empty;

    /// <summary>
    /// Коллекция сообщений, принадлежащих этому чату.
    /// Навигационное свойство Entity Framework Core.
    /// </summary>
    public ICollection<Message> Messages { get; set; } = new List<Message>();
} 