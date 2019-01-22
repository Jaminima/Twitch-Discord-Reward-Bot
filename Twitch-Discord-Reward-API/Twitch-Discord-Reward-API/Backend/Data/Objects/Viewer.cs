using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;

namespace Twitch_Discord_Reward_API.Backend.Data.Objects
{
    public class Viewer:BaseObject
    {
        public int Balance,WatchTime;
        public string TwitchID, DiscordID;
        public Currency Currency;
        public bool LiveNotifcations;

        public static Viewer FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<Viewer>();
        }

        public static Viewer FromID(int ID)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("ID",ID) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Viewer.ViewerID, Viewer.DiscordID, Viewer.TwitchID, Viewer.Balance, Viewer.CurrencyID, Viewer.WatchTime, Viewer.LiveNotifications
FROM Viewer
WHERE(((Viewer.ViewerID) = @ID));
", Params);
            if (RData.Count == 0) { return null; }
            Viewer Viewer = new Viewer();
            Viewer.ID = ID;
            Viewer.Balance = int.Parse(RData[0][3]);
            Viewer.DiscordID = RData[0][1];
            Viewer.TwitchID = RData[0][2];
            Viewer.Currency = Currency.FromID(int.Parse(RData[0][4]));
            Viewer.WatchTime = int.Parse(RData[0][5]);
            Viewer.LiveNotifcations = RData[0][6] == "True";
            return Viewer;
        }

        public static List<Viewer> FromCurrency(int CurrencyID)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("CurrencyID", CurrencyID) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Viewer.ViewerID, Viewer.DiscordID, Viewer.TwitchID, Viewer.Balance, Viewer.CurrencyID, Viewer.WatchTime, Viewer.LiveNotifications
FROM Viewer
WHERE (((Viewer.CurrencyID)=@CurrencyID));
", Params);
            List<Viewer> CurrencyBanks = new List<Viewer> { };
            foreach (String[] Item in RData)
            {
                Viewer Viewer = new Viewer();
                Viewer.ID = int.Parse(Item[0]);
                Viewer.DiscordID = Item[1];
                Viewer.TwitchID = Item[2];
                Viewer.Balance = int.Parse(Item[3]);
                Viewer.WatchTime = int.Parse(Item[5]);
                Viewer.LiveNotifcations = Item[6] == "True";
                CurrencyBanks.Add(Viewer);
            }
            return CurrencyBanks;
        }

        public static List<Viewer> FromTwitchDiscord(string DiscordID=null,string TwitchID=null)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { };
            string WhereStatment = "";
            if (DiscordID != null) { Params.Add(new OleDbParameter("DiscordID", DiscordID)); WhereStatment += "((Viewer.DiscordID)=@DiscordID)"; }
            if (TwitchID != null){
                if (WhereStatment != "") { WhereStatment += " AND "; }
                Params.Add(new OleDbParameter("TwitchID", TwitchID)); WhereStatment += "((Viewer.TwitchID)=@TwitchID)";
            }
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Viewer.ViewerID, Viewer.DiscordID, Viewer.TwitchID, Viewer.Balance, Viewer.CurrencyID, Viewer.WatchTime, Viewer.LiveNotifications
FROM Viewer
WHERE " + WhereStatment+@";
", Params);
            List<Viewer> UserBanks = new List<Viewer> { };
            foreach (String[] Item in RData)
            {
                Viewer Viewer = new Viewer();
                Viewer.ID = int.Parse(Item[0]);
                Viewer.DiscordID = Item[1];
                Viewer.TwitchID = Item[2];
                Viewer.Balance = int.Parse(Item[3]);
                Viewer.Currency = Currency.FromID(int.Parse(Item[4]));
                Viewer.WatchTime = int.Parse(Item[5]);
                Viewer.LiveNotifcations = Item[6] == "True";
                UserBanks.Add(Viewer);
            }
            return UserBanks;
        }

        public static Viewer FromTwitchDiscord(string DiscordID = null, string TwitchID = null,int CurrencyID=-1)
        {
            if (CurrencyID != -1)
            {
                List<Viewer> B = FromTwitchDiscord(DiscordID, TwitchID);
                return B.Find(x => x.Currency.ID == CurrencyID);
            }
            return null;
        }

        public bool Save()
        {
            if (FromTwitchDiscord(this.DiscordID,this.TwitchID,this.Currency.ID) == null)
            {
                List<OleDbParameter> Params = new List<OleDbParameter> {
                    new OleDbParameter("Balance",this.Balance),
                    new OleDbParameter("CurrencyID",this.Currency.ID)
                };
                string PostStatment = "",PreStatment="";
                if (DiscordID != null) { Params.Add(new OleDbParameter("DiscordID", DiscordID)); PreStatment += "DiscordID"; PostStatment += "@DiscordID"; }
                if (TwitchID != null)
                {
                    if (PostStatment != "") { PreStatment += ","; PostStatment += ","; }
                    Params.Add(new OleDbParameter("TwitchID", TwitchID)); PreStatment += "TwitchID"; PostStatment += "@TwitchID";
                }
                Init.SQLi.Execute(@"INSERT INTO Viewer (Balance, CurrencyID, " + PreStatment+ @") VALUES (@Balance, @CurrencyID, " + PostStatment+@")", Params);
                return true;
            }
            return false;
        }

        public bool Update()
        {
            if (FromID(this.ID) != null) 
            {
                List<OleDbParameter> Params = new List<OleDbParameter> {
                    new OleDbParameter("DiscordID",this.DiscordID),
                    new OleDbParameter("TwitchID",this.TwitchID),
                    new OleDbParameter("Balance",this.Balance),
                    new OleDbParameter("Notifcations",this.LiveNotifcations),
                    new OleDbParameter("WatchTime",this.WatchTime),
                    new OleDbParameter("ID",this.ID)
                };
                Init.SQLi.Execute(@"UPDATE Viewer SET Viewer.DiscordID = @DiscordID, Viewer.TwitchID = @TwitchID, Viewer.Balance = @Balance, Viewer.LiveNotifications = @Notifications, Viewer.WatchTime = @WatchTime
WHERE(((Viewer.ViewerID) = @ID));
", Params);
                return true;
            }
            else { return false; }
        }

        public void Delete()
        {
            if (FromID(this.ID) != null)
            {
                List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("ID",this.ID) };
                Init.SQLi.Execute(@"DELETE FROM Viewer
WHERE (((Viewer.ViewerID)=@ID));
",Params);
            }
        }
    }
}
