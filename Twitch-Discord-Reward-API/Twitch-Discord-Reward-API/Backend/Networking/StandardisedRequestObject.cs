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
        /* This Object places usefull and frequently used data in an easy to access set of variables inside of the object
         * This will allow for shorter code, and by placing it in an object, the data can be kept together in a very elegant manner.*/
        public string URL,Method;
        public string[] URLSegments;
        public Dictionary<string, string> URLParamaters,StateParamaters;
        public System.Collections.Specialized.NameValueCollection Headers;
        public ResponseObject ResponseObject;//By keeping the response object and request data here, we wont need to pass it seperatly to functions
        public Newtonsoft.Json.Linq.JToken RequestData;
        public HttpListenerContext Context;//We store the original data for circumstances where the data is not stored seperatly in this object
        
        public StandardisedRequestObject(HttpListenerContext Context,ResponseObject ResponseObject) // When creating the object we will require the ListenerContext and the ResponseObject that are being used
        {
            Headers = Context.Request.Headers;//Set the objects data
            URL = Context.Request.RawUrl.ToLower();
            Method = Context.Request.HttpMethod.ToLower();
            URLSegments = URL.Split("/".ToCharArray());
            URLParamaters = GetParamaters(Context.Request.RawUrl);
            if (Method == "post")//If the method is post, read the posted data into json format and store it
            {
                string StreamString = new System.IO.StreamReader(Context.Request.InputStream).ReadToEnd();
                if (StreamString != "") { RequestData = Newtonsoft.Json.Linq.JToken.Parse(StreamString); }
            }
            this.Context = Context;//Set the objects object references
            this.ResponseObject = ResponseObject;
        }

        //Convert all url paramaters into a dictionary for ease of use
        Dictionary<string, string> GetParamaters(string URL)
        {
            Dictionary<string, string> Params = new Dictionary<string, string> { };
            if (URL.Contains("?"))//Only attempt if the url does contain a ?
            {
                string[] ParamSet = URL.Split("?".ToCharArray())[1].Split("&".ToCharArray());//split the parameter string into its individual variables
                foreach (string Param in ParamSet)//Go through each variable and add the key and value into the dictionary
                {
                    string[] SplitParam = Param.Split("=".ToCharArray());
                    if (SplitParam.Length == 2)
                    {
                        Params.Add(SplitParam[0].ToLower(), SplitParam[1]);
                    }
                }
            }
            return Params;
        }

        //Converts vriables part of the url state paramater into a dictionary
        public void GetStateParams()
        {
            Dictionary<string, string> Params = new Dictionary<string, string> { };
            string[] ParamSet = this.URLParamaters["state"].Split(new string[] { "%20","+" },StringSplitOptions.None);//split the state paramater into its sub-variables
            foreach (string Param in ParamSet)//Go through each sub-variable and add the key and value into the dictionary
            {
                string[] SplitParam = Param.Split(new string[] { "%3D" },StringSplitOptions.None);
                if (SplitParam.Length == 2)
                {
                    Params.Add(SplitParam[0].ToLower(), SplitParam[1]);
                }
            }
            StateParamaters = Params;
        }
    }
}
