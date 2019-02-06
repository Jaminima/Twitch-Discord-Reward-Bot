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
            Listener = new HttpListener(); // Initalise the Listener and configure it
            Listener.Prefixes.Add("http://+:"+Backend.Init.APIConfig["Port"]+"/");
            Listener.Start();
            Listener.BeginGetContext(HandleRequest, null);//When we recive a request send to the the HandleRequest procdeure
            if (Listener.IsListening) { Console.WriteLine("Web API is now running!"); } // Report that the listener is running
        }

        static void HandleRequest(IAsyncResult Request)
        {
            new Thread(() => RequestThread(Listener.EndGetContext(Request))).Start();//Create a thread of RequestThread, in order to prevent delay in handling new requests
            Listener.BeginGetContext(HandleRequest, null); // Restart listener
        }

        static void RequestThread(HttpListenerContext Context)
        {
            string Event = Context.Request.RemoteEndPoint + " Visited " + Context.Request.RawUrl + " Using " + Context.Request.HttpMethod;
            Console.WriteLine(Event); 
            HttpListenerResponse Resp = Context.Response; // Create the Listener Response and set response parameters
            Resp.StatusCode = 200;
            Resp.ContentType = "application/json";
            ResponseObject ResponseObject = new ResponseObject(); // Create a reponse object and assign default values
            ResponseObject.Code = 400; ResponseObject.Message = "Non-Specific Bad Request";
            try
            {
                // Create a StandardisedRequestObject and provide it to the Get or Post function based on the method used by the request
                StandardisedRequestObject Req = new StandardisedRequestObject(Context, ResponseObject);
                if (Req.Method == "get") { Get.Handle(Req); }
                if (Req.Method == "post") { Post.Handle(Req); }
            }
            catch (Exception E) { Console.WriteLine(E); ResponseObject.Code = 500; ResponseObject.Message = "Internal Server Error"; } // If an unhandled error occurs set fallback values 
            byte[] ByteResponseData = Encoding.UTF8.GetBytes(ResponseObject.ToJson().ToString()); // Convert the response object into its json equivalent and then into its byte values
            try
            {
                // Send the byte response data to the requestor
                Resp.OutputStream.Write(ByteResponseData, 0, ByteResponseData.Length);
                Resp.OutputStream.Close();
            }
            catch { Console.WriteLine("Unable to send response too " + Context.Request.RemoteEndPoint); } // If we cant send the response report the error to console
        }
    }
}
