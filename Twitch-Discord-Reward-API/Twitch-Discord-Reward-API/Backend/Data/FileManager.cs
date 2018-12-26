using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_API.Backend.Data
{
    public static class FileManager
    {
        public static Newtonsoft.Json.Linq.JToken ReadFile(string FilePath)
        {
            if (System.IO.File.Exists(FilePath))
            {
                string Raw = System.IO.File.ReadAllText(FilePath);
                try { return Newtonsoft.Json.Linq.JToken.Parse(Raw); }
                catch { return null; }
            }
            return null;
        }

        public static void WriteFile(string FilePath,Newtonsoft.Json.Linq.JToken Json)
        {
            System.IO.File.WriteAllText(FilePath, Json.ToString());
        }
    }
}
