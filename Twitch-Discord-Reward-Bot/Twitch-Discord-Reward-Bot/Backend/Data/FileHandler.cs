using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_Bot.Backend.Data
{
    public static class FileHandler
    {
        public static Newtonsoft.Json.Linq.JToken ReadJSON(string FilePath)
        {
            return Newtonsoft.Json.Linq.JToken.Parse(System.IO.File.ReadAllText(FilePath));
        }


        public static void SaveJSON(string FilePath,Newtonsoft.Json.Linq.JToken Data)
        {
            try { System.IO.File.WriteAllText(FilePath, Data.ToString()); }
            catch { SaveJSON(FilePath, Data); }
        }
    }
}
