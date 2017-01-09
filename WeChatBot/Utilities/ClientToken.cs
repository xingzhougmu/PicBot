using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WeChatBot.Utilities
{
    public class ClientToken
    {
        public string access_token;
        public string expires_in;

        public ClientToken()
        {
            access_token = "";
            expires_in = "";
        }
    }
}