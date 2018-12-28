using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;

namespace Twitch_Discord_Reward_API.Backend.Data.Objects
{
    public class Bank:BaseObject
    {
        public int Balance;
        public string TwitchID, DiscordID;
        public Currency Currency;

        public static Bank FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<Bank>();
        }

        public static Bank FromID(int ID)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("ID",ID) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Bank.BankID, Bank.DiscordID, Bank.TwitchID, Bank.Balance, Bank.CurrencyID
FROM Bank
WHERE(((Bank.BankID) = @ID));
", Params);
            if (RData.Count == 0) { return null; }
            Bank Bank = new Bank();
            Bank.ID = ID;
            Bank.Balance = int.Parse(RData[0][3]);
            Bank.DiscordID = RData[0][1];
            Bank.TwitchID = RData[0][2];
            Bank.Currency = Currency.FromID(int.Parse(RData[0][3]));
            return Bank;
        }

        public static List<Bank> FromCurrency(int CurrencyID)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("CurrencyID", CurrencyID) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Bank.BankID, Bank.DiscordID, Bank.TwitchID, Bank.Balance, Bank.CurrencyID
FROM Bank
WHERE (((Bank.CurrencyID)=@CurrencyID));
", Params);
            List<Bank> CurrencyBanks = new List<Bank> { };
            foreach (String[] Item in RData)
            {
                Bank Bank = new Bank();
                Bank.ID = int.Parse(Item[0]);
                Bank.DiscordID = RData[0][1];
                Bank.TwitchID = RData[0][2];
                Bank.Balance = int.Parse(Item[3]);
                CurrencyBanks.Add(Bank);
            }
            return CurrencyBanks;
        }

        public static List<Bank> FromTwitchDiscord(string DiscordID=null,string TwitchID=null)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { };
            string WhereStatment = "";
            if (DiscordID != null) { Params.Add(new OleDbParameter("DiscordID", DiscordID)); WhereStatment += "((Bank.DiscordID)=@DiscordID)"; }
            if (TwitchID != null){
                if (WhereStatment != "") { WhereStatment += " AND "; }
                Params.Add(new OleDbParameter("TwitchID", TwitchID)); WhereStatment += "((Bank.TwitchID)=@TwitchID)";
            }
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Bank.BankID, Bank.DiscordID, Bank.TwitchID, Bank.Balance, Bank.CurrencyID
FROM Bank
WHERE "+WhereStatment+@";
", Params);
            List<Bank> UserBanks = new List<Bank> { };
            foreach (String[] Item in RData)
            {
                Bank Bank = new Bank();
                Bank.ID = int.Parse(Item[0]);
                Bank.Balance = int.Parse(Item[3]);
                Bank.Currency = Currency.FromID(int.Parse(Item[4]));
                UserBanks.Add(Bank);
            }
            return UserBanks;
        }

        public static Bank FromTwitchDiscord(string DiscordID = null, string TwitchID = null,int CurrencyID=-1)
        {
            if (CurrencyID != -1)
            {
                List<Bank> B = FromTwitchDiscord(DiscordID, TwitchID);
                return B.Find(x => x.Currency.ID == CurrencyID);
            }
            return null;
        }

        public bool Save()
        {
            if (FromTwitchDiscord(this.DiscordID,this.TwitchID).Find(x => x.Currency.ID == this.Currency.ID) == null)
            {
                List<OleDbParameter> Params = new List<OleDbParameter> {
                    new OleDbParameter("TwitchID",this.TwitchID),
                    new OleDbParameter("DiscordID",this.DiscordID),
                    new OleDbParameter("Balance",this.Balance),
                    new OleDbParameter("CurrencyID",this.Currency.ID)
                };
                Init.SQLi.Execute(@"INSERT INTO Bank (TwitchID, DiscordID, Balance, CurrencyID) VALUES (@TwitchID, @DiscordID, @Balance, @CurrencyID)", Params);
                return true;
            }
            return false;
        }
    }
}
