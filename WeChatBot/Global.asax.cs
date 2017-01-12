using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using WeChatBot.Utilities;

namespace WeChatBot
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            // get text translation api token
            AdmAccessToken admToken;
            // string headerValue;
            AdmAuthentication admAuth = new AdmAuthentication();
            try
            {
                admToken = admAuth.GetAccessToken();
                // Create a header with the access_token property of the returned token
                Utilities.Utilities.textAPIToken = "Bearer " + admToken.access_token;
                // TranslateMethod(headerValue);
            }
            catch (WebException e)
            {
                // ProcessWebException(e);
                // Console.WriteLine("Press any key to continue...");
            }
            catch (Exception ex)
            {
                // Console.WriteLine(ex.Message);
            }
        }
    }
}
