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
        public User User;
        public Currency Currency;

        public static Bank FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<Bank>();
        }

        public static Bank FromID(int ID)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("ID",ID) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Bank.BankID, Bank.UserID, Bank.Balance, Bank.CurrencyID
FROM Bank
WHERE(((Bank.BankID) = @ID));
",Params);
            if (RData.Count == 0) { return null; }
            Bank Bank = new Bank();
            Bank.ID = ID;
            Bank.Balance = int.Parse(RData[0][2]);
            Bank.User = User.FromID(int.Parse(RData[0][1]));
            Bank.Currency = Currency.FromID(int.Parse(RData[0][3]));
            return Bank;
        }

        public static List<Bank> FromCurrency(int CurrencyID)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("CurrencyID", CurrencyID) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Bank.BankID, Bank.UserID, Bank.Balance, Bank.CurrencyID
FROM Bank
WHERE (((Bank.CurrencyID)=@CurrencyID));
", Params);
            List<Bank> CurrencyBanks = new List<Bank> { };
            foreach (String[] Item in RData)
            {
                Bank Bank = new Bank();
                Bank.ID = int.Parse(Item[0]);
                Bank.User = User.FromID(int.Parse(Item[1]));
                Bank.Balance = int.Parse(Item[2]);
                CurrencyBanks.Add(Bank);
            }
            return CurrencyBanks;
        }

        public static List<Bank> FromUser(int UserID)
        {
            List<OleDbParameter> Parmas = new List<OleDbParameter> { new OleDbParameter("UserID", UserID) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Bank.BankID, Bank.UserID, Bank.Balance, Bank.CurrencyID
FROM Bank
WHERE (((Bank.UserID)=@UserID));
", Parmas);
            List<Bank> UserBanks = new List<Bank> { };
            foreach (String[] Item in RData)
            {
                Bank Bank = new Bank();
                Bank.ID = int.Parse(Item[0]);
                Bank.Balance = int.Parse(Item[2]);
                Bank.Currency = Currency.FromID(int.Parse(Item[3]));
                UserBanks.Add(Bank);
            }
            return UserBanks;
        }

        public bool Save()
        {
            if (FromUser(this.User.ID).Find(x => x.Currency.ID == this.Currency.ID) == null)
            {
                List<OleDbParameter> Params = new List<OleDbParameter> {
                    new OleDbParameter("UserID",this.User.ID),
                    new OleDbParameter("Balance",this.Balance),
                    new OleDbParameter("CurrencyID",this.Currency.ID)
                };
                Init.SQLi.Execute(@"INSERT INTO Bank (UserID, Balance, CurrencyID) VALUES (@UserID, @Balance, @CurrencyID)", Params);
                return true;
            }
            return false;
        }
    }
}
