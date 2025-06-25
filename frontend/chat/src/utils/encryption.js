import CryptoJS from 'crypto-js';

/**
 * Шифрует сообщение с помощью AES-128
 * @param {string} message - Сообщение для шифрования
 * @param {string} key - Ключ шифрования в формате Base64
 * @param {string} iv - Вектор инициализации в формате Base64
 * @returns {string} Зашифрованное сообщение в формате Base64
 * @throws {Error} Если параметры некорректны или произошла ошибка шифрования
 */
export const encryptMessage = (message, key, iv) => {
    if (!message || typeof message !== 'string') {
        throw new Error('Сообщение должно быть непустой строкой');
    }
    if (!key || typeof key !== 'string') {
        throw new Error('Ключ должен быть непустой строкой');
    }
    if (!iv || typeof iv !== 'string') {
        throw new Error('Вектор инициализации должен быть непустой строкой');
    }

    try {
        const keyWordArray = CryptoJS.enc.Base64.parse(key);
        const ivWordArray = CryptoJS.enc.Base64.parse(iv);
        
        const encrypted = CryptoJS.AES.encrypt(message, keyWordArray, {
            iv: ivWordArray,
            mode: CryptoJS.mode.CBC,
            padding: CryptoJS.pad.Pkcs7
        });
        
        const result = encrypted.toString();
        if (!result) {
            throw new Error('Ошибка при шифровании: пустой результат');
        }
        
        return result;
    } catch (error) {
        throw new Error(`Ошибка при шифровании сообщения: ${error.message}`);
    }
};

/**
 * Расшифровывает сообщение с помощью AES-128
 * @param {string} encryptedMessage - Зашифрованное сообщение в формате Base64
 * @param {string} key - Ключ шифрования в формате Base64
 * @param {string} iv - Вектор инициализации в формате Base64
 * @returns {string} Расшифрованное сообщение
 * @throws {Error} Если параметры некорректны или произошла ошибка расшифровки
 */
export const decryptMessage = (encryptedMessage, key, iv) => {
    if (!encryptedMessage || typeof encryptedMessage !== 'string') {
        throw new Error('Зашифрованное сообщение должно быть непустой строкой');
    }
    if (!key || typeof key !== 'string') {
        throw new Error('Ключ должен быть непустой строкой');
    }
    if (!iv || typeof iv !== 'string') {
        throw new Error('Вектор инициализации должен быть непустой строкой');
    }

    try {
        const keyWordArray = CryptoJS.enc.Base64.parse(key);
        const ivWordArray = CryptoJS.enc.Base64.parse(iv);
        
        const decrypted = CryptoJS.AES.decrypt(encryptedMessage, keyWordArray, {
            iv: ivWordArray,
            mode: CryptoJS.mode.CBC,
            padding: CryptoJS.pad.Pkcs7
        });
        
        const decryptedString = decrypted.toString(CryptoJS.enc.Utf8);
        if (!decryptedString) {
            throw new Error('Не удалось расшифровать сообщение');
        }
        
        return decryptedString;
    } catch (error) {
        if (error.message.includes('Malformed UTF-8 data')) {
            throw new Error('Невозможно расшифровать сообщение: неверный формат данных');
        }
        throw new Error(`Ошибка при расшифровке сообщения: ${error.message}`);
    }
}; 