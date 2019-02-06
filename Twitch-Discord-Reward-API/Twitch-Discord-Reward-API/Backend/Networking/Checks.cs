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

        public static bool IsValidID(string ID)//Check if all characters in the ID string are numbers
        {
            foreach (Char C in ID)
            {
                if (!NumberSet.Contains(C)) { return false; }
            }
            return true;
        }

        public static bool IsAlphaNumericString(string Str)//Check if all characters in the String are either numbers or letters
        {
            foreach (Char C in Str)
            {
                if (!NumberSet.Contains(C) && !LowerSet.Contains(C) && !UpperSet.Contains(C)) { return false; }
            }
            return true;
        }

        public static bool IsValidPassword(string Password)//Check if the string contains at least 1 capital,number and special
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

        public static bool IsValidValueInJsonConfig(string JsonValue)//Check if the value inside the json conforms to our valid charcter set
        {
            Char PrevC=Char.MinValue;
            int ClosableBrackets = 0;
            foreach (Char C in JsonValue)
            {
                if (!LowerSet.Contains(C) && !UpperSet.Contains(C) && !NumberSet.Contains(C) && !SpecialSet.Contains(C)) {//if the character isnt Lower,Upper,Number or special
                    if (PrevC.ToString() == "<" && C.ToString() == "@") { ClosableBrackets++; }//Where we have a start of a paramater increase the closable bracket count
                    else if (C.ToString() == ">" && ClosableBrackets > 0) { ClosableBrackets--; }//where we have the end of a paramater decreas the closable bracket count
                    else {//if it isnt the start or end of a bracket return false to indicate that it is invalid
                        return false;
                    }
                }
                PrevC = C;//Set the last character
            }
            return ClosableBrackets==0;//If we have closed all paramater brackets
        }

        public static bool IsValidEmail(string Email)//check if the string follows an email structure
        {
            int AtCount = 0;
            foreach (Char C in Email)
            {
                if (C.ToString() == "@") { AtCount++; }//Increment the amount of @s in the string
                else if (!NumberSet.Contains(C) && !LowerSet.Contains(C) && !UpperSet.Contains(C)&&C.ToString()!=".") { return false; }//if the character isnt upper,lower or number
            }
            if (AtCount != 1) { return false; }//If we have more than one @ return false to indicate it is invalid
            if (!Email.Split("@".ToCharArray())[1].Contains(".")) { return false; }//If the string after the @ doesnt contain a . return false to induicate it is invalid
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

        //Perform a recursive search of the given json, and check if the values conform to our valid character set
        public static void PerformSearch(Newtonsoft.Json.Linq.JToken Item,ref List<string> Paths,ref bool ValueIsAlphaNumeric, string CurrentPath = "")
        {
            try//Try to convert the json object to a jarray
            {
                Newtonsoft.Json.Linq.JArray J = Newtonsoft.Json.Linq.JArray.FromObject(Item);
                for (int i=0;i<J.Count;i++)//Perform a search of all items in the array
                {
                    PerformSearch(J[i],ref Paths,ref ValueIsAlphaNumeric,CurrentPath + "::");
                }
            }
            catch
            {
                try//Try to convert the json object to a jobject
                {
                    Newtonsoft.Json.Linq.JObject J = Newtonsoft.Json.Linq.JObject.FromObject(Item);
                    foreach (Newtonsoft.Json.Linq.JProperty Key in J.Properties())//Look at all properties in the jobject
                    {
                        if (Key.Value.HasValues)//If the property has further values
                        {
                            if (!Paths.Contains(CurrentPath + Key.Name + ":"))//Check if we have all ready entered the current path into the path set
                            { Paths.Add(CurrentPath + Key.Name + ":"); }
                            PerformSearch(Key.Value, ref Paths, ref ValueIsAlphaNumeric, CurrentPath + Key.Name + ":");//Perform search of items inside of the property
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
                catch//Treat the json object as a terminating value in the json
                {
                    if (!IsValidValueInJsonConfig(Item.ToString()))//check if the value conforms
                    {
                        if (!Item.ToString().StartsWith("<:") && !Item.ToString().StartsWith("<a:")) {//if it doesnt start with discord emote indicators
                            ValueIsAlphaNumeric = false; //Indicate that a value does not conform }
                    }
                }
            }
            
        }
    }
}

