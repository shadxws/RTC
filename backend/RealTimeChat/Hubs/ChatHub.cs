using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RealTimeChat.Models;
using System.Collections.Concurrent;
using RealTimeChat.Data;
using RealTimeChat.Services;
using Microsoft.EntityFrameworkCore;

namespace RealTimeChat.Hubs;

/// <summary>
/// Интерфейс, определяющий методы, которые клиент (фронтенд) должен реализовать
/// для получения сообщений и обновлений от сервера SignalR хаба.
/// </summary>
public interface IChatClient
{
    /// <summary>
    /// Метод для получения нового сообщения от сервера.
    /// </summary>
    /// <param name="userName">Имя отправителя сообщения.</param>
    /// <param name="message">Текст сообщения (уже расшифрованный).</param>
    /// <param name="timestamp">Время отправки сообщения в формате строки (например, "HH:mm").</param>
    public Task ReceiveMessage(string userName, string message, string timestamp);

    /// <summary>
    /// Метод для обновления списка пользователей, находящихся в текущем чате.
    /// </summary>
    /// <param name="users">Массив строк с именами пользователей в чате.</param>
    public Task UpdateUserList(string[] users);

    /// <summary>
    /// Метод для отображения сообщения об ошибке на клиенте.
    /// </summary>
    /// <param name="message">Текст сообщения об ошибке.</param>
    public Task ShowError(string message);

    /// <summary>
    /// Метод для передачи клиенту ключей шифрования для текущего чата.
    /// Клиент использует эти ключи для расшифровки исторических сообщений и
    /// шифрования своих сообщений перед отправкой (хотя сейчас шифрование на фронте отключено).
    /// </summary>
    /// <param name="key">Ключ шифрования в формате Base64.</param>
    /// <param name="iv">Вектор инициализации в формате Base64.</param>
    public Task SetEncryptionKeys(string key, string iv);

    /// <summary>
    /// Метод для получения системного уведомления, не являющегося ошибкой.
    /// Используется для информационных сообщений, например, об удалении чата.
    /// </summary>
    /// <param name="message">Текст системного уведомления.</param>
    public Task ReceiveSystemNotification(string message);
}

/// <summary>
/// SignalR хаб, который обрабатывает логику чата в реальном времени.
/// Взаимодействует с клиентами, базой данных и сервисом шифрования.
/// </summary>
public class ChatHub : Hub<IChatClient>
{
    private readonly ApplicationDbContext _dbContext; // Контекст базы данных для доступа к сущностям Chat и Message
    private readonly EncryptionService _encryptionService; // Сервис для выполнения операций шифрования/дешифрования

    // Статический словарь для хранения пользователей по комнатам чата.
    // Ключ: нормализованное название чата (string).
    // Значение: ConcurrentDictionary<string, string>, где ключ - ConnectionId SignalR,
    // значение - нормализованное имя пользователя.
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> RoomUsers = new();

    // Статический словарь для хранения информации о подключениях пользователей.
    // Ключ: ConnectionId SignalR (string).
    // Значение: Объект UserConnection, содержащий имя пользователя и название чата.
    private static readonly ConcurrentDictionary<string, UserConnection> Connections = new();

    /// <summary>
    /// Конструктор хаба ChatHub.
    /// Зависимости (контекст БД и сервис шифрования) внедряются через DI.
    /// </summary>
    /// <param name="dbContext">Экземпляр контекста базы данных.</param>
    /// <param name="encryptionService">Экземпляр сервиса шифрования.</param>
    public ChatHub(
        ApplicationDbContext dbContext,
        EncryptionService encryptionService)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Обрабатывает запрос клиента на присоединение к чату.
    /// Создает чат, если он не существует, отправляет ключи шифрования и историю сообщений.
    /// Добавляет пользователя в группу SignalR для чата и уведомляет других пользователей.
    /// </summary>
    /// <param name="connection">Объект, содержащий имя пользователя и название чата.</param>
    public async Task JoinChat(UserConnection connection)
    {
        try
        {
            // Проверка входных данных на null или пустоту
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrEmpty(connection.ChatRoom))
                throw new ArgumentException("Название чата не может быть пустым", nameof(connection));

            if (string.IsNullOrEmpty(connection.UserName))
                throw new ArgumentException("Имя пользователя не может быть пустым", nameof(connection));

            // Нормализация названия чата (для регистронезависимого сравнения) и имени пользователя
            var normalizedRoom = connection.ChatRoom.Trim().ToLowerInvariant();
            var normalizedUser = connection.UserName.Trim();

            // Проверка уникальности имени пользователя в пределах данного чата
            var users = RoomUsers.GetOrAdd(normalizedRoom, _ => new ConcurrentDictionary<string, string>());
            if (users.Values.Contains(normalizedUser, StringComparer.OrdinalIgnoreCase))
            {
                // Если имя занято, отправляем ошибку только инициатору запроса
                await Clients.Caller.ShowError($"Имя '{normalizedUser}' уже занято в этом чате");
                return;
            }

            // Поиск существующего чата в базе данных по нормализованному названию
            var chat = await _dbContext.Chats
                .AsNoTracking() // Используем AsNoTracking для оптимизации чтения
                .FirstOrDefaultAsync(c => c.Name == normalizedRoom);

            if (chat == null)
            {
                // Если чат не найден, создаем новый
                var (key, iv) = _encryptionService.GenerateKeyAndIV(); // Генерируем новые ключи
                chat = new Chat
                {
                    Name = normalizedRoom,
                    CreatedAt = DateTime.UtcNow,
                    EncryptionKey = key,
                    EncryptionIV = iv
                };
                _dbContext.Chats.Add(chat); // Добавляем новый чат в контекст
                await _dbContext.SaveChangesAsync(); // Сохраняем изменения в базе данных
            }

            // Отправляем клиенту сгенерированные или существующие ключи шифрования для чата
            await Clients.Caller.SetEncryptionKeys(chat.EncryptionKey, chat.EncryptionIV);

            // Добавляем пользователя в локальное хранилище активных пользователей чата по ConnectionId
            users[Context.ConnectionId] = normalizedUser;
            // Добавляем текущее SignalR подключение в группу, соответствующую названию чата
            await Groups.AddToGroupAsync(Context.ConnectionId, normalizedRoom);

            // Сохраняем информацию о подключении пользователя в локальном словаре
            Connections[Context.ConnectionId] = new UserConnection(normalizedUser, normalizedRoom);

            // Загружаем последние 100 сообщений для данного чата из базы данных
            var historyMessages = await _dbContext.Messages
                .Where(m => m.ChatId == chat.Id)
                .OrderBy(m => m.Timestamp) // Сортируем по времени для правильного порядка отображения
                .Take(100) // Ограничиваем количество загружаемых сообщений
                .ToListAsync();

            // Отправляем каждое историческое сообщение клиенту, расшифровывая его
            foreach (var msg in historyMessages)
            {
                try
                {
                    // Расшифровываем содержимое сообщения, используя ключи чата
                    var decryptedContent = _encryptionService.Decrypt(msg.Content, chat.EncryptionKey, chat.EncryptionIV);
                    // Отправляем расшифрованное сообщение клиенту
                    await Clients.Caller.ReceiveMessage(msg.SenderId, decryptedContent, msg.Timestamp.ToString("HH:mm"));
                }
                catch (Exception decryptEx)
                {
                    // Логируем ошибку расшифровки и отправляем клиенту сообщение об ошибке
                    Console.WriteLine($"Ошибка при расшифровке исторического сообщения {msg.Id}: {decryptEx.Message}");
                    await Clients.Caller.ReceiveMessage("System", "[Ошибка расшифровки сообщения]", msg.Timestamp.ToString("HH:mm"));
                }
            }

            // Отправляем системное сообщение в чат о присоединении нового пользователя
            await Clients.Group(normalizedRoom)
                .ReceiveMessage("System", $"{normalizedUser} присоединился к чату", DateTime.Now.ToString("HH:mm"));

            // Обновляем список пользователей для всех клиентов в чате
            await UpdateUsersInRoom(normalizedRoom);
        }
        catch (Exception ex)
        {
            // В случае любой ошибки при подключении отправляем ошибку только инициатору запроса
            await Clients.Caller.ShowError($"Ошибка при подключении к чату: {ex.Message}");
        }
    }

    /// <summary>
    /// Обрабатывает отправку нового сообщения от клиента в чат.
    /// Шифрует сообщение, сохраняет его в базе данных и рассылает всем пользователям в чате.
    /// </summary>
    /// <param name="message">Текст отправляемого сообщения (в открытом виде).</param>
    public async Task SendMessage(string message)
    {
        try
        {
            // Проверка содержимого сообщения на пустоту
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Сообщение не может быть пустым", nameof(message));

            // Получаем информацию о подключении текущего пользователя по ConnectionId
            if (!Connections.TryGetValue(Context.ConnectionId, out var connection))
            {
                // Если информация о подключении не найдена (редкий случай, но возможен)
                await Clients.Caller.ShowError("Ошибка: соединение не найдено");
                return;
            }

            // Нормализуем название чата и находим чат в базе данных
            var normalizedRoom = connection.ChatRoom.Trim().ToLowerInvariant();
            var chat = await _dbContext.Chats
                .AsNoTracking() // Оптимизация для чтения
                .FirstOrDefaultAsync(c => c.Name == normalizedRoom);

            if (chat == null)
            {
                // Если чат не найден (что не должно произойти при нормальном флоу)
                await Clients.Caller.ShowError("Ошибка: чат не найден");
                return;
            }

            // Форматируем текущее время и шифруем сообщение
            var timestamp = DateTime.Now.ToString("HH:mm");
            var encryptedMessage = _encryptionService.Encrypt(message, chat.EncryptionKey, chat.EncryptionIV);

            // Создаем объект сообщения для сохранения в базе данных
            var dbMessage = new Message
            {
                ChatId = chat.Id,
                SenderId = connection.UserName,
                Content = encryptedMessage, // Сохраняем зашифрованный текст
                Timestamp = DateTime.UtcNow // Используем UTC для сохранения времени
            };

            try
            {
                // Добавляем сообщение в контекст БД и сохраняем изменения
                _dbContext.Messages.Add(dbMessage);
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Обрабатываем ошибки при сохранении в БД
                await Clients.Caller.ShowError($"Ошибка при сохранении сообщения: {ex.Message}");
                return;
            }

            // Отправляем ОРИГИНАЛЬНОЕ (незашифрованное) сообщение всем клиентам в группе чата
            // Клиенты больше не дешифруют сообщения после последнего изменения на фронте.
            await Clients.Group(normalizedRoom).ReceiveMessage(connection.UserName, message, timestamp);
        }
        catch (Exception ex)
        {
            // В случае любой ошибки при отправке сообщения отправляем ошибку только инициатору запроса
            await Clients.Caller.ShowError($"Ошибка при отправке сообщения: {ex.Message}");
        }
    }

    /// <summary>
    /// Обрабатывает событие отключения пользователя от SignalR хаба.
    /// Удаляет пользователя из локального хранилища и уведомляет других пользователей в чате.
    /// </summary>
    /// <param name="exception">Исключение, вызвавшее отключение (если есть).</param>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            // Пытаемся получить информацию о подключении по ConnectionId
            if (Connections.TryRemove(Context.ConnectionId, out var connection))
            {
                // Если информация о подключении найдена и успешно удалена
                var normalizedRoom = connection.ChatRoom.Trim().ToLowerInvariant();
                var normalizedUser = connection.UserName.Trim();

                // Удаляем пользователя из локального словаря пользователей чата
                if (RoomUsers.TryGetValue(normalizedRoom, out var users))
                {
                    users.TryRemove(Context.ConnectionId, out _);
                    // Если после удаления в комнате не осталось пользователей, удаляем саму комнату из словаря RoomUsers
                    if (users.IsEmpty)
                    {
                        RoomUsers.TryRemove(normalizedRoom, out _);
                    }
                }

                // Отправляем системное сообщение в чат о том, что пользователь покинул чат
                await Clients.Group(normalizedRoom)
                    .ReceiveMessage("System", $"{normalizedUser} покинул чат", DateTime.Now.ToString("HH:mm"));

                // Обновляем список пользователей для оставшихся клиентов в чате
                await UpdateUsersInRoom(normalizedRoom);
            }
        }
        catch (Exception ex)
        {
            // Логируем любые ошибки, возникшие при отключении, но не отправляем их клиенту
            Console.WriteLine($"Ошибка при отключении пользователя: {ex.Message}");
        }

        // Вызываем базовый метод OnDisconnectedAsync
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Вспомогательный метод для получения актуального списка пользователей в определенном чате
    /// и отправки этого списка всем клиентам в этой группе.
    /// </summary>
    /// <param name="room">Нормализованное название чата.</param>
    private async Task UpdateUsersInRoom(string room)
    {
        // Пытаемся получить словарь пользователей для данной комнаты
        if (RoomUsers.TryGetValue(room, out var users))
        {
            // Получаем уникальные имена пользователей из значений словаря и преобразуем в массив
            var userList = users.Values.Distinct().ToArray();
            // Отправляем обновленный список пользователей всем клиентам в группе чата
            await Clients.Group(room).UpdateUserList(userList);
        }
    }

    /// <summary>
    /// Обрабатывает запрос клиента на удаление чата.
    /// Удаляет чат и связанные с ним сообщения из базы данных, а также очищает локальное состояние.
    /// Уведомляет всех пользователей в удаляемом чате.
    /// </summary>
    /// <param name="chatRoom">Название чата, который нужно удалить.</param>
    public async Task DeleteChat(string chatRoom)
    {
        try
        {
            // Проверка названия чата на пустоту
            if (string.IsNullOrEmpty(chatRoom))
                throw new ArgumentException("Название чата не может быть пустым", nameof(chatRoom));

            // Нормализация названия чата
            var normalizedRoom = chatRoom.Trim().ToLowerInvariant();

            // Поиск чата в базе данных для удаления
            var chatToDelete = await _dbContext.Chats
                .FirstOrDefaultAsync(c => c.Name == normalizedRoom);

            if (chatToDelete == null)
            {
                // Если чат не найден, отправляем ошибку инициатору запроса
                await Clients.Caller.ShowError($"Ошибка: Чат '{chatRoom}' не найден");
                return;
            }

            // Удаляем чат из контекста базы данных.
            // Благодаря настройке OnDelete(DeleteBehavior.Cascade) в ApplicationDbContext,
            // все сообщения, связанные с этим чатом, будут удалены автоматически при сохранении изменений.
            _dbContext.Chats.Remove(chatToDelete);
            await _dbContext.SaveChangesAsync(); // Сохраняем изменения в базе данных (выполняется удаление)

            // Удаляем информацию о чате и пользователях из локальных словарей в памяти хаба.
            // Это необходимо, чтобы клиенты, находящиеся в этом чате, были отключены или перенаправлены.
            if (RoomUsers.TryRemove(normalizedRoom, out var usersInRoom))
            {
                // Удаляем записи о подключениях из словаря Connections для всех пользователей, которые были в этом чате
                foreach (var connectionId in usersInRoom.Keys)
                {
                    Connections.TryRemove(connectionId, out _);
                }
            }

            // Отправляем системное уведомление всем клиентам, которые БЫЛИ в этой группе,
            // что чат был удален. Это уведомление обрабатывается фронтендом как информационное.
            await Clients.Group(normalizedRoom).ReceiveSystemNotification($"Чат '{chatRoom}' был удален.");

            // TODO: Возможно, стоит добавить логику принудительного отключения клиентов из группы SignalR после удаления чата.
        }
        catch (Exception ex)
        {
            // Логируем ошибку на сервере и отправляем сообщение об ошибке инициатору запроса на удаление
            Console.WriteLine($"Ошибка при удалении чата '{chatRoom}': {ex.Message}");
            await Clients.Caller.ShowError($"Ошибка при удалении чата '{chatRoom}': {ex.Message}");
        }
    }
}