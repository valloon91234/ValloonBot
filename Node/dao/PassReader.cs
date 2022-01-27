using System;
using System.Collections.Generic;

namespace dao
{
    interface IReader
    {
        IEnumerable<PassModel> Passwords(String host = null);
        IEnumerable<Cookie> Cookies(String host = null);
        string BrowserName { get; }
    }
}