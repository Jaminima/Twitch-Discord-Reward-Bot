using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;

namespace Twitch_Discord_Reward_API.Backend.Data.Objects
{
    public class User : BaseObject
    {
        public string DiscordID, TwitchID;

        public User FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<User>();
        }

        public static User FromID(int ID)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("ID", ID) };
            List<string[]> RData = Init.SQLi.ExecuteReader(@"SELECT Users.UserID, Users.TwitchID, Users.DiscordID
FROM Users
WHERE (((Users.UserID)=@ID));
",Params);
            if (RData.Count == 0) { return null; }
            User User = new User();
            User.ID = ID;
            User.TwitchID = RData[0][1];
            User.DiscordID = RData[0][2];
            return User;
        }

        public static User FromTwitchDiscord(string TwitchID=null,string DiscordID=null)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { };
            string WhereQuery = "";
            if (TwitchID != null) { Params.Add(new OleDbParameter("TwitchID", TwitchID)); WhereQuery += "(((Users.TwitchID)=@TwitchID))"; }
            if (DiscordID != null) {
                if (WhereQuery != "") { WhereQuery += " AND "; }
                Params.Add(new OleDbParameter("DiscordID", DiscordID)); WhereQuery += "(((Users.DiscordID)=@DiscordID))";
            }
            List<string[]> RData = Init.SQLi.ExecuteReader(@"SELECT Users.UserID, Users.TwitchID, Users.DiscordID
FROM Users
WHERE "+WhereQuery+@";
",Params);
            if (RData.Count == 0) { return null; }
            User User = new User();
            User.ID = int.Parse(RData[0][0]);
            User.TwitchID = RData[0][1];
            User.DiscordID = RData[0][2];
            return User;
        }
    }
}
