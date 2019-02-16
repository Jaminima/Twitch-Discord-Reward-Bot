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
        public bool LiveNotifcations,DontReward;

        public static Viewer FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<Viewer>();
        }

        public static Viewer FromID(int ID)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("ID",ID) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Viewer.ViewerID, Viewer.DiscordID, Viewer.TwitchID, Viewer.Balance, Viewer.CurrencyID, Viewer.WatchTime, Viewer.LiveNotifications, Viewer.DontReward
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
            Viewer.DontReward = RData[0][7] == "True";
            return Viewer;
        }

        public static List<Viewer> FromCurrency(int CurrencyID,string OrderBy = "None")
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("CurrencyID", CurrencyID) };
            string Command = @"SELECT Viewer.ViewerID, Viewer.DiscordID, Viewer.TwitchID, Viewer.Balance, Viewer.CurrencyID, Viewer.WatchTime, Viewer.LiveNotifications, Viewer.DontReward
FROM Viewer
WHERE (((Viewer.CurrencyID)=@CurrencyID))
";
            if (OrderBy == "Balance") { Command += "ORDER BY Viewer.Balance DESC"; }
            if (OrderBy == "WatchTime") { Command += "ORDER BY Viewer.WatchTime DESC"; }
            Command += ";";
            List<String[]> RData = Init.SQLi.ExecuteReader(Command, Params);
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
                Viewer.DontReward = Item[7] == "True";
                CurrencyBanks.Add(Viewer);
            }
            return CurrencyBanks;
        }

        public static List<Viewer> FromTwitchDiscord(string DiscordID=null,string TwitchID=null)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { };
            string WhereStatment = "";
            if (DiscordID != null) { Params.Add(new OleDbParameter("DiscordID", DiscordID)); WhereStatment += "((Viewer.DiscordID)=@DiscordID)"; }//Add the DiscordID paramater if DiscordID isnt null
            if (TwitchID != null){//If TwitchID isnt null
                if (WhereStatment != "") { WhereStatment += " AND "; }//If weve already added DiscordID we add AND into the statment
                Params.Add(new OleDbParameter("TwitchID", TwitchID)); WhereStatment += "((Viewer.TwitchID)=@TwitchID)";//Add the TwitchID paramater
            }
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Viewer.ViewerID, Viewer.DiscordID, Viewer.TwitchID, Viewer.Balance, Viewer.CurrencyID, Viewer.WatchTime, Viewer.LiveNotifications, Viewer.DontReward
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
                Viewer.DontReward = Item[7] == "True";
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

        public static bool Increment(List<string> DiscordIDs = null, List<string> TwitchIDs=null,int BalanceIncrementBy=0,int WatchTimeIncrementBy=0)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("BalanceIncrement", BalanceIncrementBy),new OleDbParameter("WatchTimeIncrement",WatchTimeIncrementBy) };
            string WhereStatement = "";
            int i = 0;
            foreach(string DID in DiscordIDs)
            {
                Params.Add(new OleDbParameter("DiscordID" + i, DID));
                if (WhereStatement != "") { WhereStatement += " OR "; }
                WhereStatement += "Viewer.DiscordID=@DiscordID" + i;
                i++;
            }
            foreach (string TID in TwitchIDs)
            {
                Params.Add(new OleDbParameter("TwitchID" + i, TID));
                if (WhereStatement != "") { WhereStatement += " OR "; }
                WhereStatement += "Viewer.TwitchID=@TwitchID" + i;
                i++;
            }
            Init.SQLi.Execute(@"UPDATE Viewer SET Viewer.Balance = Viewer.Balance + @BalanceIncrement, Viewer.WatchTime = Viewer.WatchTime + @WatchTimeIncrement
WHERE (((Viewer.DontReward)=False) AND (" + WhereStatement+@"));
", Params);
            return true;
        }

        public bool Save()
        {
            //Check if DiscordID or TwitchID is already in the database
            if (FromTwitchDiscord(this.DiscordID,this.TwitchID,this.Currency.ID) == null)
            {
                List<OleDbParameter> Params = new List<OleDbParameter> {
                    new OleDbParameter("Balance",this.Balance),
                    new OleDbParameter("CurrencyID",this.Currency.ID)
                };
                //Set the sql paramaters
                string PostStatment = "",PreStatment="";
                //If DiscorID isnt null, we add it to our params and value statments
                if (DiscordID != null) { Params.Add(new OleDbParameter("DiscordID", DiscordID)); PreStatment += "DiscordID"; PostStatment += "@DiscordID"; }
                //If TwitchID isnt null, we add it to our params and value statments
                if (TwitchID != null)
                {
                    //If we have already added to our statments we will need a comma to seperate the values
                    if (PostStatment != "") { PreStatment += ","; PostStatment += ","; }
                    Params.Add(new OleDbParameter("TwitchID", TwitchID)); PreStatment += "TwitchID"; PostStatment += "@TwitchID";
                }
                Init.SQLi.Execute(@"INSERT INTO Viewer (Balance, CurrencyID, " + PreStatment+ @") VALUES (@Balance, @CurrencyID, " + PostStatment+@")", Params);
                //insert the viewer into the table
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
                    new OleDbParameter("DontReward",this.DontReward),
                    new OleDbParameter("WatchTime",this.WatchTime),
                    new OleDbParameter("ID",this.ID)
                };
                Init.SQLi.Execute(@"UPDATE Viewer SET Viewer.DiscordID = @DiscordID, Viewer.TwitchID = @TwitchID, Viewer.Balance = @Balance, Viewer.LiveNotifications = @Notifications, Viewer.DontReward = @DontReward, Viewer.WatchTime = @WatchTime
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
