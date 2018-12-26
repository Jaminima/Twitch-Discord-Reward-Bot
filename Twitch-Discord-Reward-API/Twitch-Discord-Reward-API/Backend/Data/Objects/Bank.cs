﻿using System;
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

        public Bank FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<Bank>();
        }

        public static Bank FromID(int ID,ObjectSet ChildOf=ObjectSet.None)
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
            if (ChildOf != ObjectSet.Currency) { }
            if (ChildOf != ObjectSet.User) { Bank.User = User.FromID(int.Parse(RData[0][1])); }
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
                Bank.Currency = null;//Must replace
                UserBanks.Add(Bank);
            }
            return UserBanks;
        }
    }
}
