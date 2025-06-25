namespace RealTimeChat.Models;

/// <summary>
/// Модель, представляющая информацию о подключении пользователя к определенному чату.
/// Используется для временного хранения информации о активных пользователях в памяти хаба.
/// </summary>
public class UserConnection
{
    /// <summary>
    /// Имя пользователя, связанного с этим подключением.
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// Название чата, к которому подключен пользователь.
    /// </summary>
    public string ChatRoom { get; set; }

    /// <summary>
    /// Создает новый экземпляр UserConnection.
    /// </summary>
    /// <param name="userName">Имя пользователя.</param>
    /// <param name="chatRoom">Название чата.</param>
    public UserConnection(string userName, string chatRoom)
    {
        UserName = userName;
        ChatRoom = chatRoom;
    }
}