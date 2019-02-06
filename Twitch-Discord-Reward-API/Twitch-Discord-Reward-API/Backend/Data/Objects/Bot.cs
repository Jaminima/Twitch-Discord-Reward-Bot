using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;

namespace Twitch_Discord_Reward_API.Backend.Data.Objects
{
    public class Bot : BaseObject
    {
        public Currency Currency; //Define variables to replicate the Bot table
        public string AccessToken, RefreshToken, BotName;
        public DateTime TokenRefreshDateTime;
        public Login OwnerLogin;
        public bool IsSuperBot=false;

        public static Bot FromJson(Newtonsoft.Json.Linq.JToken Json)//Convert a json into a Bot object
        {
            return Json.ToObject<Bot>();
        }

        public static Bot FromID(int ID,bool WithSecretData=false)//All Single item From functions follow a similar structure
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("ID",ID) };//Create a set of paramaters for the SQL query
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Bots.BotID, Bots.CurrencyID, Bots.AccessToken, Bots.TokenRefreshDateTime, Bots.RefreshToken, Bots.LoginID, Bots.IsSuperBot, Bots.BotName
FROM Bots
WHERE (((Bots.BotID)=@ID));
", Params);//Select table data from the table, where the BotsID matches the ID paramater
            if (RData.Count == 0) { return null; }//Check we have at least 1 item in the returned sql results
            Bot Bot = new Bot();//Create a new bot object
            Bot.ID = int.Parse(RData[0][0]);//Set the bots variables using the sql results
            if (RData[0][1] != "") { Bot.Currency = Currency.FromID(int.Parse(RData[0][1])); }
            if (WithSecretData)//Only add this information if WithSecretData is set to true
            {
                Bot.AccessToken = RData[0][2];
                Bot.TokenRefreshDateTime = DateTime.Parse(RData[0][3]);
                Bot.RefreshToken = RData[0][4];
            }
            Bot.BotName = RData[0][7];
            Bot.IsSuperBot = RData[0][6] == "True";
            Bot.OwnerLogin = Login.FromID(int.Parse(RData[0][5]));
            return Bot;//Return the bot
        }

        public static List<Bot> FromLogin(int LoginID, bool WithSecretData = false)//All List item from functions follow a similar structure too the single item functions
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("LoginID",LoginID) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Bots.BotID, Bots.CurrencyID, Bots.AccessToken, Bots.TokenRefreshDateTime, Bots.RefreshToken, Bots.LoginID, Bots.IsSuperBot, Bots.BotName
FROM Bots
WHERE (((Bots.LoginID)=@LoginID));
", Params);
            List<Bot> Bots = new List<Bot> { };//By not returning null and instead returning an empty list, we remove the necesity to check for a null object, in place of an empty list
            foreach (String[] Item in RData)//Instead of only creating a single object, we loop through all items in the sql results
            {
                Bot Bot = new Bot();
                Bot.ID = int.Parse(Item[0]);
                if (Item[1] != "") { Bot.Currency = Currency.FromID(int.Parse(Item[1])); }
                if (WithSecretData)
                {
                    Bot.AccessToken = Item[2];
                    Bot.TokenRefreshDateTime = DateTime.Parse(Item[3]);
                    Bot.RefreshToken = Item[4];
                    Bot.IsSuperBot = Item[6] == "True";
                }
                Bot.BotName = Item[7];
                Bots.Add(Bot);//And we add each object into our list of objects
            }
            return Bots;//return the list of objects
        }

        public static List<Bot> FromCurrency(int CurrencyID,bool WithSecretData = false)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("CurrencyID",CurrencyID) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Bots.BotID, Bots.CurrencyID, Bots.AccessToken, Bots.TokenRefreshDateTime, Bots.RefreshToken, Bots.LoginID, Bots.IsSuperBot, Bots.BotName
FROM Bots
WHERE (((Bots.CurrencyID)=@CurrencyID));
", Params);
            List<Bot> Bots = new List<Bot> { };
            foreach (String[] Item in RData)
            {
                Bot Bot = new Bot();
                Bot.ID = int.Parse(Item[0]);
                Bot.OwnerLogin = Login.FromID(int.Parse(Item[5]));
                Bot.IsSuperBot = Item[6] == "True";
                Bot.BotName = Item[7];
                Bots.Add(Bot);
            }
            return Bots;
        }

        public bool Save()
        {
            this.AccessToken = Networking.TokenSystem.CreateToken(32);
            this.RefreshToken = Networking.TokenSystem.CreateToken(64);
            this.TokenRefreshDateTime = DateTime.Now;
            List<OleDbParameter> Params = new List<OleDbParameter> {
                new OleDbParameter("LoginID",this.OwnerLogin.ID),
                new OleDbParameter("AccessToken",Init.ScryptEncoder.Encode(this.AccessToken)),
                new OleDbParameter("RefreshToken",Init.ScryptEncoder.Encode(this.RefreshToken)),
                new OleDbParameter("TokenRefreshDateTime",this.TokenRefreshDateTime.ToString()),
                new OleDbParameter("BotName",this.BotName)
            };
            Init.SQLi.Execute(@"INSERT INTO Bots (CurrencyID, LoginID, AccessToken, RefreshToken, TokenRefreshDateTime, BotName) VALUES (NULL, @LoginID, @AccessToken, @RefreshToken, @TokenRefreshDateTime, @BotName)", Params);
            return true;
        }

        public bool UpdateCurrency()//Change the Bots associtated currency id
        {
            if (FromID(this.ID) != null)//Check if the Bot appears in the database
            {
                List<OleDbParameter> Params = new List<OleDbParameter> {
                    new OleDbParameter("CurrencyID",this.Currency.ID),
                    new OleDbParameter("ID",this.ID)
                };//Set the sql paramaters
                Init.SQLi.Execute(@"UPDATE Bots SET Bots.CurrencyID = @CurrencyID
WHERE (((Bots.BotID) = @ID));
", Params);//Change the CurrencyID for the BotID
                return true;
            }//Report if the currency was updated, or if it failed
            return false;
        }

        public bool PerformRefresh()//Refresh the Access and Refresh Tokens
        {
            if (FromID(this.ID) != null)//Check if the Bot appears in the database
            {
                this.AccessToken = Networking.TokenSystem.CreateToken(32);//Change the Access and Refresh Tokens along with the RefreshDateTime
                this.TokenRefreshDateTime = DateTime.Now;
                this.RefreshToken = Networking.TokenSystem.CreateToken(64);
                List<OleDbParameter> Params = new List<OleDbParameter>
                {
                    new OleDbParameter("AccessToken",Init.ScryptEncoder.Encode(this.AccessToken)),
                    new OleDbParameter("TokenRefreshDateTime",this.TokenRefreshDateTime.ToString()),
                    new OleDbParameter("RefreshToken",Init.ScryptEncoder.Encode(this.RefreshToken)),
                    new OleDbParameter("ID",this.ID)
                };//Set the sql paramaters
                Init.SQLi.Execute(@"UPDATE Bots SET Bots.AccessToken = @AccessToken, Bots.TokenRefreshDateTime = @TokenRefreshDateTime, Bots.RefreshToken = @RefreshToken
WHERE (((Bots.BotID) = @ID));
", Params);//Update the Access+Refresh Token and TokenRefreshDateTime for the BotID
                return true;
            }//Report if the refresh was completed successfully
            return false;
        }

        public void Delete()
        {
            if (FromID(this.ID) != null)//Check if the Bot appears in the database
            {
                List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("ID", this.ID) };
                Init.SQLi.Execute(@"DELETE FROM Bots
WHERE (((Bots.BotID)=@ID));
", Params);
                //Delete entry where the BotID matches
            }
        }
    }
}
