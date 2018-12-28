using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace Twitch_Discord_Reward_API.Backend.Networking
{
    public class StandardisedRequestObject
    {
        public string URL,Method;
        public string[] URLSegments;
        public System.Collections.Specialized.NameValueCollection Headers;
        public ResponseObject ResponseObject;
        public HttpListenerContext Context;
        
        public StandardisedRequestObject(HttpListenerContext Context,ResponseObject ResponseObject)
        {
            Headers = Context.Request.Headers;
            URL = Context.Request.RawUrl.ToLower();
            Method = Context.Request.HttpMethod.ToLower();
            URLSegments = URL.Split("/".ToCharArray());
            this.Context = Context;
            this.ResponseObject = ResponseObject;
        }
    }
}
