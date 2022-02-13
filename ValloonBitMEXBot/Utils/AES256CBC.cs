using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace Valloon.Trading.Utils
{
    public class AES256CBC
    {
        private static readonly Encoding encoding = Encoding.UTF8;
        private readonly RijndaelManaged AES;

        public AES256CBC(byte[] key)
        {
            AES = new RijndaelManaged
            {
                KeySize = 256,
                BlockSize = 128,
                Padding = PaddingMode.PKCS7,
                Mode = CipherMode.CBC,
                Key = key
            };
        }

        public string Encrypt(string plainText)
        {
            byte[] buffer = encoding.GetBytes(plainText);
            AES.GenerateIV();
            using (var Encryptor = AES.CreateEncryptor())
            {
                string encryptedText = Convert.ToBase64String(Encryptor.TransformFinalBlock(buffer, 0, buffer.Length));
                String mac = BitConverter.ToString(HmacSHA256(Convert.ToBase64String(AES.IV) + encryptedText, AES.Key)).Replace("-", "").ToLower();
                var keyValues = new Dictionary<string, object>
                {
                    { "iv", Convert.ToBase64String(AES.IV) },
                    { "value", encryptedText },
                    { "mac", mac },
                };
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                return Convert.ToBase64String(encoding.GetBytes(serializer.Serialize(keyValues)));
            }
        }

        public string Decrypt(string plainText)
        {
            byte[] base64Decoded = Convert.FromBase64String(plainText);
            string base64DecodedStr = encoding.GetString(base64Decoded);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            var payload = serializer.Deserialize<Dictionary<string, string>>(base64DecodedStr);
            AES.IV = Convert.FromBase64String(payload["iv"]);
            byte[] buffer = Convert.FromBase64String(payload["value"]);
            using (var Decryptor = AES.CreateDecryptor())
                return encoding.GetString(Decryptor.TransformFinalBlock(buffer, 0, buffer.Length));
        }

        private byte[] HmacSHA256(String data, byte[] key)
        {
            using (HMACSHA256 hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(encoding.GetBytes(data));
            }
        }

        public void Dispose()
        {
            AES.Dispose();
        }
    }
}
