﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;

namespace Twitch_Discord_Reward_API.Backend.Data.Objects
{
    public class Currency : BaseObject
    {
        public Login OwnerLogin;
        public Newtonsoft.Json.Linq.JToken LoginConfig, CommandConfig;

        public void LoadConfigs(bool WithLogin = false)//Load the confuartion files into the bot object
        {
            //Only load the login config if WithLogin is true
            if (WithLogin) { LoginConfig = FileManager.ReadFile("./Data/CurrencyConfigs/" + ID + "/Login.config.json"); }
            CommandConfig = FileManager.ReadFile("./Data/CurrencyConfigs/" + ID + "/Command.config.json");
        }

        public static Currency FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<Currency>();
        }

        public static Currency FromID(int ID)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("ID",ID) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Currency.CurrencyID, Currency.LoginID
FROM [Currency]
WHERE (((Currency.CurrencyID)=@ID));
", Params);
            if (RData.Count == 0) { return null; }
            Currency Currency = new Currency();
            Currency.ID = ID;
            Currency.LoadConfigs();
            Currency.OwnerLogin = Login.FromID(int.Parse(RData[0][1]));
            return Currency;
        }

        public static List<Currency> FromLogin(int UserID)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("UserID",UserID) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Currency.CurrencyID, Currency.LoginID
FROM [Currency]
WHERE (((Currency.LoginID)=@UserID));
", Params);
            List<Currency> Currencies = new List<Currency> { };
            foreach (String[] Item in RData)
            {
                Currency Currency = new Currency();
                Currency.ID = int.Parse(Item[0]);
                Currency.LoadConfigs();
                Currencies.Add(Currency);
            }
            return Currencies;
        }

        public static List<Currency> All(bool WithSecretData = false)
        {
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Currency.CurrencyID, Currency.LoginID
FROM [Currency];");
            List<Currency> Currencies = new List<Currency> { };
            foreach (String[] Item in RData)
            {
                Currency Currency = new Currency();
                Currency.ID = int.Parse(Item[0]);
                Currency.OwnerLogin = Login.FromID(int.Parse(RData[0][1]));
                Currency.LoadConfigs(WithSecretData);
                Currencies.Add(Currency);
            }
            return Currencies;
        }

        public bool Save()
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("LoginID",this.OwnerLogin.ID) };
            Init.SQLi.Execute(@"INSERT INTO [Currency] (LoginID) VALUES (@LoginID)", Params);
            Currency C = FromLogin(this.OwnerLogin.ID).Last();
            //Create a directory for the configuration files
            System.IO.Directory.CreateDirectory("./Data/CurrencyConfigs/" + C.ID);
            //Copy the example config files into the directory
            System.IO.File.Copy("./Data/DefaultConfigs/Command.config.json", "./Data/CurrencyConfigs/" + C.ID+ "/Command.config.json");
            System.IO.File.Copy("./Data/DefaultConfigs/Login.config.json", "./Data/CurrencyConfigs/" + C.ID + "/Login.config.json");
            return true;
        }

        public void UpdateConfigs()
        {
            //Overwrite the current contents of the configuration files with the new config data
            FileManager.WriteFile("./Data/CurrencyConfigs/" + this.ID + "/Command.config.json",this.CommandConfig.ToString());
            FileManager.WriteFile("./Data/CurrencyConfigs/" + this.ID + "/Login.config.json", this.LoginConfig.ToString());
        }

        public void Delete()
        {
            if (FromID(this.ID) != null)
            {
                //Delete the folder with the currency configuration files in
                System.IO.Directory.Delete("./Data/CurrencyConfigs/" + this.ID,true);
                List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("ID", this.ID) };
                Init.SQLi.Execute(@"DELETE FROM [Currency]
WHERE (((Currency.CurrencyID)=@ID));
", Params);
            }
        }
    }
}
