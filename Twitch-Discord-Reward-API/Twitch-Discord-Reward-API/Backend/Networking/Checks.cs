using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_API.Backend.Networking
{
    public static class Checks
    {
        static Char[] NSet = "0123456789".ToCharArray();
        public static bool IsValidID(string ID)
        {
            foreach (Char C in ID)
            {
                if (!NSet.Contains(C)) { return false; }
            }
            return true;
        }

        public static bool JSONLayoutCompare(Newtonsoft.Json.Linq.JToken Layout, Newtonsoft.Json.Linq.JToken Data)
        {
            bool MissingItem = false;
            List<string> LayoutPaths = new List<string> { }, DataPaths = new List<string> { };
            PerformSearch(Layout,ref LayoutPaths); PerformSearch(Data, ref DataPaths);
            foreach (string Path in LayoutPaths)
            {
                if (!DataPaths.Contains(Path)) { MissingItem = true; break; }
            }
            foreach (string Path in DataPaths.Where(x => x.Contains(":::")))
            {
                if (!LayoutPaths.Contains(Path)) { MissingItem = true; break; }
            }
            return !MissingItem;
        }

        public static void PerformSearch(Newtonsoft.Json.Linq.JToken Item,ref List<string> Paths,string CurrentPath="")
        {
            try
            {
                Newtonsoft.Json.Linq.JArray J = Newtonsoft.Json.Linq.JArray.FromObject(Item);
                for (int i=0;i<J.Count;i++)
                {
                    PerformSearch(J[i],ref Paths,CurrentPath+"::");
                }
            }
            catch
            {
                Newtonsoft.Json.Linq.JObject J = Newtonsoft.Json.Linq.JObject.FromObject(Item);
                foreach (Newtonsoft.Json.Linq.JProperty Key in J.Properties())
                {
                    if (Key.Value.HasValues)
                    {
                        Paths.Add(CurrentPath + Key.Name + ":");
                        PerformSearch(Key.Value, ref Paths, CurrentPath + Key.Name + ":");
                    }
                    else
                    {
                        Paths.Add(CurrentPath + Key.Name + ":");
                    }
                }
            }
            
        }
    }
}
