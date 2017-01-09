// #define DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
// using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using WeChatBot.Utilities;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Queue; // Namespace for Queue storage types
using System.Configuration;
using System.IO;
using System.Web;
using System.Threading.Tasks;

namespace WeChatBot.Controllers
{
    public class WeChatRelayController : ApiController
    {
                
        /// <summary>
        /// Path: /api/wechat/
        /// 微信后台验证地址（使用Get），微信后台的“接口配置信息”的Url填写如：https://wechatbotdemo.azurewebsites.net/api/WeChatRelay/
        /// 注意返回echostr字符串类型微信只接受“application/x-www-form-urlencoded”，直接返回是不被接受的
        /// </summary>
        /// <param name="signature"></param>
        /// <param name="timestamp"></param>
        /// <param name="nonce"></param>
        /// <param name="echostr"></param>
        /// <returns></returns>
        public HttpResponseMessage Get(string signature, string timestamp, string nonce, string echostr)
        {
            if (Utilities.Utilities.CheckSource(signature, timestamp, nonce))
            {
                var result = new StringContent(echostr, UTF8Encoding.UTF8, "application/x-www-form-urlencoded"); // 注意返回echostr字符串类型微信只接受“application/x-www-form-urlencoded”，直接返回是不被接受的
                var response = new HttpResponseMessage { Content = result };
                return response;
            }
            return new HttpResponseMessage();
        }

        public async Task<string> Post()
        {
            // Create a message and add it to the queue.
            HttpContext context = HttpContext.Current;
            StreamReader sr = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
            string msg_received = sr.ReadToEnd();
#if DEBUG
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a container.
            CloudQueue queue = queueClient.GetQueueReference("wechatbot");

            // Create the queue if it doesn't already exist
            queue.CreateIfNotExists();
            CloudQueueMessage message = new CloudQueueMessage(msg_received);
            queue.AddMessage(message);
#endif

            sr.Close();
            string echo_str;
            try
            {
                echo_str = await Utilities.Utilities.MessageConsumer(msg_received);
            }
            catch (Exception e)
            {
                echo_str = e.InnerException.ToString();
            }

            // queue.AddMessage(new CloudQueueMessage(echo_str));

            return echo_str;
        }
    }
}
