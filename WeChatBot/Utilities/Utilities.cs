using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Text;
using System.Security.Cryptography;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Xml.Serialization;
using System.Xml;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;

namespace WeChatBot.Utilities
{
    public static class Utilities
    {
        /// <summary>
        /// Microsoft Computer Vision API key.
        /// </summary>
        private static readonly string ApiKey = ConfigurationManager.AppSettings["MicrosoftVisionApiKey"];

        /// <summary>
        /// The set of visual features we want from the Vision API.
        /// </summary>
        private static readonly VisualFeature[] VisualFeatures = { VisualFeature.Description };

        /// <summary>
        /// 检验是否来自微信的签名
        /// </summary>
        /// <param name="signature"></param>
        /// <param name="timestamp"></param>
        /// <param name="nonce"></param>
        /// <returns>Boolean Value: True or False</returns>
        public static bool CheckSource(string signature, string timestamp, string nonce)
        {
            var str = string.Empty;
            var token = ConfigurationManager.AppSettings["token"];
            var parameter = new List<string> { token, timestamp, nonce };
            parameter.Sort();
            var parameterStr = parameter[0] + parameter[1] + parameter[2];
            var tempStr = GetSHA1(parameterStr).Replace("-", "").ToLower();
            if (tempStr == signature)
                return true;

            return false;
        }

        /// <summary>
        /// SHA1加密
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetSHA1(string input)
        {
            var output = string.Empty;
            var sha1 = new SHA1CryptoServiceProvider();
            var inputBytes = UTF8Encoding.UTF8.GetBytes(input);
            var outputBytes = sha1.ComputeHash(inputBytes);
            sha1.Clear();
            output = BitConverter.ToString(outputBytes);
            return output;
        }

        /// <summary>
        /// 获取access_token
        /// access_token是公众号的全局唯一接口调用凭据，公众号调用各接口时都需使用access_token。开发者需要进行妥善保存。
        /// access_token的存储至少要保留512个字符空间。
        /// access_token的有效期目前为2个小时，需定时刷新，重复获取将导致上次获取的access_token失效。
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        public static ClientToken Get_AccessToken(string appid, string secret)
        {
            string request_url = $"https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={appid}&secret={secret}";

            WebRequest request = WebRequest.Create(request_url);
            WebResponse response = request.GetResponse();

            if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
            {
                Stream dataStream = response.GetResponseStream();
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ClientToken));
                ClientToken token = (ClientToken)ser.ReadObject(dataStream);

                dataStream.Close();
                response.Close();
                return token;
            }
            else
            {
                response.Close();
                return new ClientToken();
            }

        }

        /// <summary>
        /// Deserialize message string to TextMessage object
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static T Deserialize<T>(string message)
        {
            try
            {
                // "<xml xmlns=''> was not expected."
                XmlRootAttribute xRoot = new XmlRootAttribute();
                xRoot.ElementName = "xml";
                xRoot.IsNullable = true;

                XmlSerializer ser =
                    new XmlSerializer(typeof(T), xRoot);
                MemoryStream stream = new MemoryStream();
                StreamWriter writer = new StreamWriter(stream);
                writer.Write(message);
                writer.Flush();
                stream.Position = 0;
                
                T relayMsg = (T)ser.Deserialize(stream);
                writer.Close();
                return relayMsg;
            }
            catch (Exception e)
            {
                return default(T);
            }

        }
        /// <summary>
        /// Wechat Message Handler
        /// </summary>
        /// <param name="msg">Message String received from Wechat server</param>
        /// <returns>Echo message sent back to wechat server</returns>
        public static async Task<string> MessageConsumer(string msg)
        {
            string echo_str = string.Empty;
            string echo_content;

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(msg);

            try
            {
                string MsgType = xml.SelectSingleNode("/xml/MsgType").InnerText;
                switch (MsgType.Trim().ToLower())
                {
                    case "text":
                        TextMessage msg_normal = Deserialize<TextMessage>(msg);
                        echo_content = $"You sent: {msg_normal.Content}. I'm more of a visual person. " +
                                        "Try sending me an image or an image URL";
                        echo_str = string.Format(Constant.echoTextMsg, msg_normal.FromUserName, msg_normal.ToUserName, msg_normal.CreateTime,
                                                       echo_content);
                        break;
                    case "image":
                        ImageMessage msg_image = Deserialize<ImageMessage>(msg);
                        // Azure Computer Version API to detect image caption
                        echo_content = await GetCaptionAsync(msg_image.PicUrl);
                        echo_str = string.Format(Constant.echoTextMsg, msg_image.FromUserName, msg_image.ToUserName, msg_image.CreateTime,
                                                       echo_content);

                        break;
                    case "event": 
                        switch (xml.SelectSingleNode("/xml/Event").InnerText)
                        {
                            case "subscribe":
                                EventMessage msg_event = Deserialize<EventMessage>(msg);
                                echo_content = "Welcome to PicBot! I can understand the content of any image" +
                                         " and try to describe it as well as any human. Try sending me an image.";
                                echo_str = string.Format(Constant.echoTextMsg, msg_event.FromUserName, msg_event.ToUserName, msg_event.CreateTime,
                                                               echo_content);
                                break;
                            case "unsubscribe":
                                break;
                            default:
                                break;
                        }
                        
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {

            }

            return echo_str;
        }

        /// <summary>
        /// Gets the caption of an image URL.
        /// <remarks>
        /// This method calls <see cref="IVisionServiceClient.AnalyzeImageAsync(string, string[])"/> and
        /// returns the first caption from the returned <see cref="AnalysisResult.Description"/>
        /// The client is created in Mooncake, be careful about the apiroot "https://api.cognitive.azure.cn/vision/v1.0".
        /// </remarks>
        /// </summary>
        /// <param name="url">The URL to an image.</param>
        /// <returns>Description if caption found, null otherwise.</returns>
        public static async Task<string> GetCaptionAsync(string url)
        {
            var client = new VisionServiceClient(ApiKey, "https://api.cognitive.azure.cn/vision/v1.0");
            var result = await client.AnalyzeImageAsync(url, VisualFeatures);
            return ProcessAnalysisResult(result);
        }

        /// <summary>
        /// Gets the caption of the image from an image stream.
        /// <remarks>
        /// This method calls <see cref="IVisionServiceClient.AnalyzeImageAsync(Stream, string[])"/> and
        /// returns the first caption from the returned <see cref="AnalysisResult.Description"/>
        /// The client is created in Mooncake, be careful about the apiroot "https://api.cognitive.azure.cn/vision/v1.0".
        /// </remarks>
        /// </summary>
        /// <param name="stream">The stream to an image.</param>
        /// <returns>Description if caption found, null otherwise.</returns>
        public static async Task<string> GetCaptionAsync(Stream stream)
        {
            var client = new VisionServiceClient(ApiKey, "https://api.cognitive.azure.cn/vision/v1.0");
            var result = await client.AnalyzeImageAsync(stream, VisualFeatures);
            return ProcessAnalysisResult(result);
        }

        /// <summary>
        /// Processes the analysis result.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>The caption if found, error message otherwise.</returns>
        private static string ProcessAnalysisResult(AnalysisResult result)
        {
            string message = result?.Description?.Captions.FirstOrDefault()?.Text;

            return string.IsNullOrEmpty(message) ?
                        "Couldn't find a caption for this one" :
                        "I think it's " + message;
        }

    }
}