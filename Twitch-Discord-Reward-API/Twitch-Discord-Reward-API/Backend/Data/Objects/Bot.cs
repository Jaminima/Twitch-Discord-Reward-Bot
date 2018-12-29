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
        public Currency Currency;
        public string AccessToken, RefreshToken;
        public DateTime TokenRefreshDateTime;
        public Login OwnerLogin;

        public static Bot FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<Bot>();
        }

        public static Bot FromID(int ID,bool WithSecretData=false)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("ID",ID) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Bots.BotID, Bots.CurrencyID, Bots.AccessToken, Bots.TokenRefreshDateTime, Bots.RefreshToken, Bots.LoginID
FROM Bots
WHERE (((Bots.BotID)=@ID));
", Params);
            if (RData.Count == 0) { return null; }
            Bot Bot = new Bot();
            Bot.ID = int.Parse(RData[0][0]);
            Bot.Currency = Currency.FromID(int.Parse(RData[0][1]));
            if (WithSecretData)
            {
                Bot.AccessToken = RData[0][2];
                Bot.TokenRefreshDateTime = DateTime.Parse(RData[0][3]);
                Bot.RefreshToken = RData[0][4];
            }
            Bot.OwnerLogin = Login.FromID(int.Parse(RData[0][5]));
            return Bot;
        }

        public static List<Bot> FromLogin(int LoginID, bool WithSecretData = false)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("LoginID",LoginID) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Bots.BotID, Bots.CurrencyID, Bots.AccessToken, Bots.TokenRefreshDateTime, Bots.RefreshToken, Bots.LoginID
FROM Bots
WHERE (((Bots.LoginID)=@LoginID));
", Params);
            List<Bot> Bots = new List<Bot> { };
            foreach (String[] Item in RData)
            {
                Bot Bot = new Bot();
                Bot.ID = int.Parse(Item[0]);
                Bot.Currency = Currency.FromID(int.Parse(Item[1]));
                if (WithSecretData)
                {
                    Bot.AccessToken = RData[0][2];
                    Bot.TokenRefreshDateTime = DateTime.Parse(RData[0][3]);
                    Bot.RefreshToken = RData[0][4];
                }
                Bots.Add(Bot);
            }
            return Bots;
        }

        public static List<Bot> FromCurrency(int CurrencyID,bool WithSecretData = false)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("CurrencyID",CurrencyID) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Bots.BotID, Bots.CurrencyID, Bots.AccessToken, Bots.TokenRefreshDateTime, Bots.RefreshToken, Bots.LoginID
FROM Bots
WHERE (((Bots.CurrencyID)=@CurrencyID));
", Params);
            List<Bot> Bots = new List<Bot> { };
            foreach (String[] Item in RData)
            {
                Bot Bot = new Bot();
                Bot.ID = int.Parse(Item[0]);
                Bot.OwnerLogin = Login.FromID(int.Parse(Item[5]));
                Bots.Add(Bot);
            }
            return Bots;
        }

        public static Bot FromAccessToken(string AccessToken)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("AccessToken", AccessToken) };
            List<string[]> RData = Init.SQLi.ExecuteReader(@"SELECT Bots.BotID, Bots.CurrencyID, Bots.AccessToken, Bots.TokenRefreshDateTime, Bots.RefreshToken, Bots.LoginID
FROM Bots
WHERE ((Bots.AccessToken)=@AccessToken);
", Params);
            if (RData.Count == 0) { return null; }
            Bot Bot = new Bot();
            Bot.ID = int.Parse(RData[0][0]);
            Bot.Currency = Currency.FromID(int.Parse(RData[0][1]));
            Bot.AccessToken = RData[0][2];
            Bot.TokenRefreshDateTime = DateTime.Parse(RData[0][3]);
            Bot.RefreshToken = RData[0][4];
            Bot.OwnerLogin = Login.FromID(int.Parse(RData[0][5]));

            if ((int)((TimeSpan)(DateTime.Now - Bot.TokenRefreshDateTime)).TotalMinutes < 10) { return Bot; }
            else { return null; }
        }

        public static Bot FromRefreshToken(string RefreshToken)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("RefreshToken", RefreshToken) };
            List<string[]> RData = Init.SQLi.ExecuteReader(@"SELECT Bots.BotID, Bots.CurrencyID, Bots.AccessToken, Bots.TokenRefreshDateTime, Bots.RefreshToken, Bots.LoginID
FROM Bots
WHERE ((Bots.RefreshToken)=@RefreshToken);
", Params);
            if (RData.Count == 0) { return null; }
            Bot Bot = new Bot();
            Bot.ID = int.Parse(RData[0][0]);
            Bot.Currency = Currency.FromID(int.Parse(RData[0][1]));
            Bot.AccessToken = RData[0][2];
            Bot.TokenRefreshDateTime = DateTime.Parse(RData[0][3]);
            Bot.RefreshToken = RData[0][4];
            Bot.OwnerLogin = Login.FromID(int.Parse(RData[0][5]));

            if ((int)((TimeSpan)(DateTime.Now - Bot.TokenRefreshDateTime)).TotalMinutes < 10) { return Bot; }
            else { return null; }
        }

        public bool Save()
        {
            this.AccessToken = Networking.TokenSystem.CreateToken(32);
            this.RefreshToken = Networking.TokenSystem.CreateToken(64);
            this.TokenRefreshDateTime = DateTime.Now;
            List<OleDbParameter> Params = new List<OleDbParameter> {
                new OleDbParameter("CurrencyID",this.Currency.ID),
                new OleDbParameter("LoginID",this.OwnerLogin.ID),
                new OleDbParameter("AccessToken",this.AccessToken),
                new OleDbParameter("RefreshToken",this.RefreshToken),
                new OleDbParameter("TokenRefreshDateTime",this.TokenRefreshDateTime.ToString())
            };
            Init.SQLi.Execute(@"INSERT INTO Bots (CurrencyID, LoginID, AccessToken, RefreshToken, TokenRefreshDateTime) VALUES (@CurrencyID, @LoginID, @AccessToken, @RefreshToken, @TokenRefreshDateTime)", Params);
            return true;
        }

        public bool PerformRefresh()
        {
            if (FromID(this.ID) != null)
            {
                this.AccessToken = Networking.TokenSystem.CreateToken(32);
                this.TokenRefreshDateTime = DateTime.Now;
                this.RefreshToken = Networking.TokenSystem.CreateToken(64);
                List<OleDbParameter> Params = new List<OleDbParameter>
                {
                    new OleDbParameter("CurrencyID",this.Currency.ID),
                    new OleDbParameter("AccessToken",this.AccessToken),
                    new OleDbParameter("TokenRefreshDateTime",this.TokenRefreshDateTime.ToString()),
                    new OleDbParameter("RefreshToken",this.RefreshToken),
                    new OleDbParameter("ID",this.ID)
                };
                Init.SQLi.Execute(@"UPDATE Bots SET Bots.CurrencyID = @CurrencyID, Bots.AccessToken = @AccessToken, Bots.TokenRefreshDateTime = @TokenRefreshDateTime, Bots.RefreshToken = @RefreshToken
WHERE(((Bots.BotID) = @ID));
                ", Params);
                return true;
            }
            return false;
        }
    }
}
