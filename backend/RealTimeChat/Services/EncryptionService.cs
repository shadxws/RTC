using System.Security.Cryptography;
using System.Text;

namespace RealTimeChat.Services;

/// <summary>
/// Сервис для выполнения операций шифрования и дешифрования сообщений чата.
/// Использует алгоритм AES-128.
/// </summary>
public class EncryptionService
{
    /// <summary>
    /// Генерирует новую пару случайных ключа и вектора инициализации (IV) для алгоритма AES-128.
    /// </summary>
    /// <returns>Кортеж, содержащий ключ и вектор инициализации в формате строки Base64.</returns>
    public (string key, string iv) GenerateKeyAndIV()
    {
        // Создаем новый экземпляр AES
        using var aes = Aes.Create();
        // Устанавливаем размер ключа 128 бит
        aes.KeySize = 128;
        // Генерируем случайный криптографический ключ
        aes.GenerateKey();
        // Генерируем случайный вектор инициализации
        aes.GenerateIV();

        // Возвращаем ключ и IV в формате Base64 строк
        return (
            Convert.ToBase64String(aes.Key),
            Convert.ToBase64String(aes.IV)
        );
    }

    /// <summary>
    /// Шифрует заданный открытый текст с использованием указанного ключа и вектора инициализации AES-128.
    /// </summary>
    /// <param name="plainText">Исходный текст, который нужно зашифровать.</param>
    /// <param name="keyBase64">Ключ шифрования в формате Base64 строки.</param>
    /// <param name="ivBase64">Вектор инициализации в формате Base64 строки.</param>
    /// <returns>Зашифрованный текст в формате Base64 строки.</returns>
    /// <exception cref="ArgumentException">Выбрасывается, если входные параметры неверны или произошла ошибка шифрования.</exception>
    public string Encrypt(string plainText, string keyBase64, string ivBase64)
    {
        try
        {
            // Преобразуем ключ и IV из Base64 строк в массивы байтов
            var key = Convert.FromBase64String(keyBase64);
            var iv = Convert.FromBase64String(ivBase64);

            // Создаем новый экземпляр AES с указанными ключом и IV
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            // Создаем объект шифрования (encryptor)
            using var encryptor = aes.CreateEncryptor();
            // Создаем поток памяти для хранения зашифрованных данных
            using var msEncrypt = new MemoryStream();
            // Создаем криптографический поток, который шифрует данные при записи
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            // Создаем StreamWriter для записи текста в криптографический поток
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                // Записываем исходный текст в поток шифрования
                swEncrypt.Write(plainText);
            }

            // Возвращаем зашифрованные данные из потока памяти в формате Base64 строки
            return Convert.ToBase64String(msEncrypt.ToArray());
        }
        catch (Exception ex)
        {
            // Обрабатываем ошибки шифрования
            throw new ArgumentException("Ошибка при шифровании сообщения", ex);
        }
    }

    /// <summary>
    /// Дешифрует заданный зашифрованный текст с использованием указанного ключа и вектора инициализации AES-128.
    /// </summary>
    /// <param name="cipherText">Зашифрованный текст в формате Base64 строки.</param>
    /// <param name="keyBase64">Ключ шифрования в формате Base64 строки.</param>
    /// <param name="ivBase64">Вектор инициализации в формате Base64 строки.</param>
    /// <returns>Расшифрованный текст в виде строки.</returns>
    /// <exception cref="ArgumentException">Выбрасывается, если входные параметры неверны или произошла ошибка дешифрования.</exception>
    public string Decrypt(string cipherText, string keyBase64, string ivBase64)
    {
        try
        {
            // Преобразуем ключ, IV и зашифрованный текст из Base64 строк в массивы байтов
            var key = Convert.FromBase64String(keyBase64);
            var iv = Convert.FromBase64String(ivBase64);
            var cipherBytes = Convert.FromBase64String(cipherText);

            // Создаем новый экземпляр AES с указанными ключом и IV
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            // Создаем объект дешифрования (decryptor)
            using var decryptor = aes.CreateDecryptor();
            // Создаем поток памяти из зашифрованных данных
            using var msDecrypt = new MemoryStream(cipherBytes);
            // Создаем криптографический поток, который дешифрует данные при чтении
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            // Создаем StreamReader для чтения расшифрованного текста из криптографического потока
            using var srDecrypt = new StreamReader(csDecrypt);

            // Читаем и возвращаем расшифрованный текст
            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            // Обрабатываем ошибки дешифрования
            throw new ArgumentException("Ошибка при дешифровании сообщения", ex);
        }
    }
} 