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
            if (System.IO.File.Exists(FilePath))//Check if the file exists
            {
                string Raw = System.IO.File.ReadAllText(FilePath);//Read the file
                try { return Newtonsoft.Json.Linq.JToken.Parse(Raw); }//Try to convert the file contents to json form and pass it back
                catch { return null; }//If it cant be converted return null
            }
            return null;
        }

        public static void WriteFile(string FilePath,Newtonsoft.Json.Linq.JToken Json)
        {
            System.IO.File.WriteAllText(FilePath, Json.ToString());//Write the json into the given file
        }
    }
}
