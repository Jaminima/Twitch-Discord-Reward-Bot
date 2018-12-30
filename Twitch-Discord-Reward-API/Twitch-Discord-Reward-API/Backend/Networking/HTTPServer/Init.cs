using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;

namespace Twitch_Discord_Reward_API.Backend.Networking.HTTPServer
{
    public static class Init
    {
        static HttpListener Listener;
        public static void Start()
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add("http://+:"+Backend.Init.APIConfig["Port"]+"/");
            Listener.Start();
            Listener.BeginGetContext(HandleRequest, null);
            if (Listener.IsListening) { Console.WriteLine("Web API is now running!"); }
        }

        static void HandleRequest(IAsyncResult Request)
        {
            new Thread(() => RequestThread(Listener.EndGetContext(Request))).Start();
            Listener.BeginGetContext(HandleRequest, null);
        }

        static void RequestThread(HttpListenerContext Context)
        {
            string Event = Context.Request.RemoteEndPoint + " Visited " + Context.Request.RawUrl + " Using " + Context.Request.HttpMethod;
            Console.WriteLine(Event);
            HttpListenerResponse Resp = Context.Response;
            Resp.StatusCode = 200;
            Resp.ContentType = "application/json";
            ResponseObject ResponseObject = new ResponseObject();
            ResponseObject.Code = 400; ResponseObject.Message = "Non-Specific Bad Request";
            try
            {
                StandardisedRequestObject Req = new StandardisedRequestObject(Context, ResponseObject);
                if (Req.Method == "get") { Get.Handle(Req); }
                if (Req.Method == "post") { Post.Handle(Req); }
            }
            catch (Exception E) { Console.WriteLine(E); ResponseObject.Code = 500; ResponseObject.Message = "Internal Server Error"; }
            byte[] ByteResponseData = Encoding.UTF8.GetBytes(ResponseObject.ToJson().ToString());
            try
            {
                Resp.OutputStream.Write(ByteResponseData, 0, ByteResponseData.Length);
                Resp.OutputStream.Close();
            }
            catch { Console.WriteLine("Unable to send response too " + Context.Request.RemoteEndPoint); }
        }
    }
}
