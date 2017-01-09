using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WeChatBot.Utilities
{
    public static class Constant
    {
        public static string echoTextMsg = @"<xml>
                                <ToUserName><![CDATA[{0}]]></ToUserName>
                                <FromUserName><![CDATA[{1}]]></FromUserName>
                                <CreateTime>{2}</CreateTime>
                                <MsgType><![CDATA[text]]></MsgType>
                                <Content><![CDATA[{3}]]></Content>
                               </xml>";

        public static string echoImageMsg = @"<xml>
                                <ToUserName><![CDATA[{0}]]></ToUserName>
                                <FromUserName><![CDATA[{1}]]></FromUserName>
                                <CreateTime>{2}</CreateTime>
                                <MsgType><![CDATA[image]]></MsgType>
                                <Image>
                                <MediaId><![CDATA[{3}]]></MediaId>
                                </Image>
                               </xml>";
    }
}