using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Threading;
using System.Net;
using System.IO;

namespace WeChatBot.Utilities
{
    public class AdmAuthentication
    {
        public static readonly string TokenServiceUrl = "https://api.cognitive.microsoft.com/sts/v1.0/issueToken";
        private string requestURL;
        private AdmAccessToken token;
        private Timer accessTokenRenewer;
        //Access token expires every 10 minutes. Renew it every 9 minutes only.
        private const int RefreshTokenDuration = 9;
        public AdmAuthentication()
        {
            string textAPIKey = ConfigurationManager.AppSettings["MicrosoftTextApiKey"];
            this.requestURL = string.Format("{0}?Subscription-Key={1}", TokenServiceUrl, textAPIKey); // Ref: http://docs.microsofttranslator.com/oauth-token.html
            this.token = HttpPost(this.requestURL);
            //renew the token every specified minutes
            accessTokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback), this, TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
        }

        public AdmAccessToken GetAccessToken()
        {
            return this.token;
        }

        private void RenewAccessToken()
        {
            AdmAccessToken newAccessToken = HttpPost(this.requestURL);
            //swap the new token with old one
            //Note: the swap is thread unsafe
            this.token = newAccessToken;
            Console.WriteLine(string.Format("Renewed token is: {0}", this.token.access_token));
        }

        private void OnTokenExpiredCallback(object stateInfo)
        {
            try
            {
                RenewAccessToken();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Failed renewing access token. Details: {0}", ex.Message));
            }
            finally
            {
                try
                {
                    accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
                }
            }
        }

        private AdmAccessToken HttpPost(string requestURL)
        {
            //Prepare OAuth request 
            WebRequest webRequest = WebRequest.Create(requestURL);
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "POST";
            webRequest.ContentLength = 0;

            WebResponse response = null;
            response = webRequest.GetResponse();
            using (Stream stream = response.GetResponseStream())
            {
                //DataContractSerializer dcs = new DataContractSerializer(Type.GetType("System.String"));
                //string responseStr = (string)dcs.ReadObject(stream);

                StreamReader sr = new StreamReader(stream);
                string responseStr = sr.ReadToEnd();

                AdmAccessToken token = new AdmAccessToken();
                token.access_token = responseStr;
                return token;
            }
        }
    }

    public class AdmAccessToken
    {
        public string access_token { get; set; }
        public string expires_in { get; set; }
    }
}