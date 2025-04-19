using System;
using System.Security.Cryptography;
using System.Text;

namespace SmartFin.Services
{
    public class CryptoHelper
    {
        private readonly string _securityKey;

        public CryptoHelper(IConfiguration configuration)
        {
            // Получаем ключ из конфигурации или используем фиксированный
            _securityKey = configuration["Security:InvitationKey"] ?? "YourDefaultSecretKey123!";
        }

        // Шифрование данных
        public string Encrypt(string data)
        {
            try
            {
                // Преобразуем строку в байты
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);

                // Создаем хеш ключа
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_securityKey));
                byte[] keyBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(_securityKey));

                // Используем простой XOR для шифрования
                byte[] result = new byte[dataBytes.Length];
                for (int i = 0; i < dataBytes.Length; i++)
                {
                    result[i] = (byte)(dataBytes[i] ^ keyBytes[i % keyBytes.Length]);
                }

                // Преобразуем результат в URL-безопасную строку
                return Convert.ToBase64String(result)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace('=', '.');
            }
            catch
            {
                return null;
            }
        }

        // Дешифрование данных
        public string Decrypt(string encryptedData)
        {
            try
            {
                // Восстанавливаем оригинальную строку Base64
                encryptedData = encryptedData
                    .Replace('-', '+')
                    .Replace('_', '/')
                    .Replace('.', '=');

                // Получаем байты из Base64
                byte[] encryptedBytes = Convert.FromBase64String(encryptedData);

                // Создаем хеш ключа
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_securityKey));
                byte[] keyBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(_securityKey));

                // Используем XOR для дешифрования
                byte[] resultBytes = new byte[encryptedBytes.Length];
                for (int i = 0; i < encryptedBytes.Length; i++)
                {
                    resultBytes[i] = (byte)(encryptedBytes[i] ^ keyBytes[i % keyBytes.Length]);
                }

                // Преобразуем результат в строку
                return Encoding.UTF8.GetString(resultBytes);
            }
            catch
            {
                return null;
            }
        }
    }
}