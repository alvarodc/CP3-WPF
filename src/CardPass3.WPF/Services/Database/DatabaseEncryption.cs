using System.Security.Cryptography;
using System.Text;

namespace CardPass3.WPF.Services.Database
{
    /// <summary>
    /// Cifrado AES-256-GCM para la contraseña de conexión a la base de datos.
    /// Reemplaza el RijndaelManaged del sistema original (obsoleto en .NET 8).
    /// 
    /// Formato almacenado (hex): [12 bytes nonce][16 bytes tag][n bytes ciphertext]
    /// </summary>
    internal static class DatabaseEncryption
    {
        // Misma passphrase que el sistema original para compatibilidad
        private const string PassPhrase = "AmDtyX@a3m.eu";
        private const int NonceSize  = 12;  // AES-GCM nonce estándar
        private const int TagSize    = 16;  // AES-GCM tag estándar
        private const int KeySize    = 32;  // 256 bits

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                throw new CryptographicException("La contraseña no puede estar vacía.");

            var key        = DeriveKey(PassPhrase);
            var nonce      = new byte[NonceSize];
            var tag        = new byte[TagSize];
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipher     = new byte[plainBytes.Length];

            RandomNumberGenerator.Fill(nonce);

            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plainBytes, cipher, tag);

            // Concatenar: nonce + tag + ciphertext → hex string
            var result = new byte[NonceSize + TagSize + cipher.Length];
            Buffer.BlockCopy(nonce,   0, result, 0,                    NonceSize);
            Buffer.BlockCopy(tag,     0, result, NonceSize,            TagSize);
            Buffer.BlockCopy(cipher,  0, result, NonceSize + TagSize,  cipher.Length);

            return Convert.ToHexString(result);
        }

        public static string Decrypt(string hexCipherText)
        {
            if (string.IsNullOrWhiteSpace(hexCipherText))
                throw new CryptographicException("El texto cifrado no puede estar vacío.");

            var data = Convert.FromHexString(hexCipherText);

            if (data.Length < NonceSize + TagSize)
                throw new CryptographicException("Datos cifrados corruptos o con formato incorrecto.");

            var key    = DeriveKey(PassPhrase);
            var nonce  = data[..NonceSize];
            var tag    = data[NonceSize..(NonceSize + TagSize)];
            var cipher = data[(NonceSize + TagSize)..];
            var plain  = new byte[cipher.Length];

            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, cipher, tag, plain);

            return Encoding.UTF8.GetString(plain);
        }

        /// <summary>
        /// Deriva una clave de 256 bits desde la passphrase usando PBKDF2-SHA256.
        /// Usamos una sal fija derivada de la passphrase (para compatibilidad con ficheros existentes).
        /// </summary>
        private static byte[] DeriveKey(string passPhrase)
        {
            // Sal determinista basada en la passphrase — permite descifrar sin almacenar la sal
            var salt = SHA256.HashData(Encoding.UTF8.GetBytes(passPhrase + "_cp3_salt"));
            return Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(passPhrase),
                salt,
                iterations: 100_000,
                HashAlgorithmName.SHA256,
                KeySize);
        }
    }
}
