using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_API.Backend.Networking
{
    public static class Checks
    {
        static Char[] NumberSet = "0123456789".ToCharArray(),
            LowerSet="abcdefghijklmnopqrstuvwxyz ".ToCharArray(),
            UpperSet="ABCDEFGHIJKLMNOPQRSTUVWXYZ ".ToCharArray(),
            SpecialSet="!\"\n£$%^&*()-_=+{}[]@'#~;:,./`¬? ".ToCharArray();

        public static bool IsValidID(string ID)
        {
            foreach (Char C in ID)
            {
                if (!NumberSet.Contains(C)) { return false; }
            }
            return true;
        }

        public static bool IsAlphaNumericString(string Str)
        {
            foreach (Char C in Str)
            {
                if (!NumberSet.Contains(C) && !LowerSet.Contains(C) && !UpperSet.Contains(C)) { return false; }
            }
            return true;
        }

        public static bool IsValidPassword(string Password)
        {
            bool HasNumeric = false, HasCapital = false, HasSpecial = false;
            foreach (Char C in Password)
            {
                if (UpperSet.Contains(C)) { HasCapital = true; }
                else if (NumberSet.Contains(C)) { HasNumeric = true; }
                else if (SpecialSet.Contains(C)) { HasSpecial = true; }
            }
            return HasCapital && HasNumeric && HasSpecial;
        }

        public static bool IsValidValueInJsonConfig(string JsonValue)
        {
            Char PrevC=Char.MinValue;
            int ClosableBrackets = 0;
            foreach (Char C in JsonValue)
            {
                if (!LowerSet.Contains(C) && !UpperSet.Contains(C) && !NumberSet.Contains(C) && !SpecialSet.Contains(C)) {
                    if (PrevC.ToString() == "@" && C.ToString() == "<") { ClosableBrackets++; }
                    else if (C.ToString() == ">" && ClosableBrackets > 0) { ClosableBrackets--; }
                    else {
                        return false;
                    }
                }
                PrevC = C;
            }
            return true;
        }

        public static bool IsValidEmail(string Email)
        {
            int AtCount = 0;
            foreach (Char C in Email)
            {
                if (C.ToString() == "@") { AtCount++; }
                else if (!NumberSet.Contains(C) && !LowerSet.Contains(C) && !UpperSet.Contains(C)&&C.ToString()!=".") { return false; }
            }
            if (AtCount != 1) { return false; }
            if (!Email.Split("@".ToCharArray())[1].Contains(".")) { return false; }
            return true;
        }

        public static bool JSONLayoutCompare(Newtonsoft.Json.Linq.JToken Layout, Newtonsoft.Json.Linq.JToken Data)
        {
            bool MissingItem = false, LayoutValuesAreAlphaNumeric=true, DataValuesAreAlphaNumeric=true;
            List<string> LayoutPaths = new List<string> { }, DataPaths = new List<string> { };
            PerformSearch(Layout,ref LayoutPaths, ref LayoutValuesAreAlphaNumeric); PerformSearch(Data, ref DataPaths,ref DataValuesAreAlphaNumeric);
            foreach (string Path in LayoutPaths)
            {
                if (!DataPaths.Contains(Path)) { MissingItem = true; break; }
            }
            foreach (string Path in DataPaths.Where(x => x.Contains(":::")))
            {
                if (!LayoutPaths.Contains(Path)) { MissingItem = true; break; }
            }
            return !MissingItem&&DataValuesAreAlphaNumeric;
        }

        public static void PerformSearch(Newtonsoft.Json.Linq.JToken Item,ref List<string> Paths,ref bool ValueIsAlphaNumeric, string CurrentPath = "")
        {
            try
            {
                Newtonsoft.Json.Linq.JArray J = Newtonsoft.Json.Linq.JArray.FromObject(Item);
                for (int i=0;i<J.Count;i++)
                {
                    PerformSearch(J[i],ref Paths,ref ValueIsAlphaNumeric,CurrentPath + "::");
                }
            }
            catch
            {
                try
                {
                    Newtonsoft.Json.Linq.JObject J = Newtonsoft.Json.Linq.JObject.FromObject(Item);
                    foreach (Newtonsoft.Json.Linq.JProperty Key in J.Properties())
                    {
                        if (Key.Value.HasValues)
                        {
                            if (!Paths.Contains(CurrentPath + Key.Name + ":"))
                            { Paths.Add(CurrentPath + Key.Name + ":"); }
                            PerformSearch(Key.Value, ref Paths, ref ValueIsAlphaNumeric, CurrentPath + Key.Name + ":");
                        }
                        else
                        {
                            if (!Paths.Contains(CurrentPath + Key.Name + ":"))
                            { Paths.Add(CurrentPath + Key.Name + ":"); }
                            if (!IsValidValueInJsonConfig(Key.Value.ToString()))
                            {
                                if (!Key.Value.ToString().StartsWith("<:") && !Key.Value.ToString().StartsWith("<a:")) {
                                    ValueIsAlphaNumeric = false; }
                            }
                        }
                    }
                }
                catch
                {
                    if (!IsValidValueInJsonConfig(Item.ToString()))
                    {
                        if (!Item.ToString().StartsWith("<:")) {
                            ValueIsAlphaNumeric = false; }
                    }
                }
            }
            
        }
    }
}
