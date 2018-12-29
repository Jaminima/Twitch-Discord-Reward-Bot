using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;

namespace Twitch_Discord_Reward_API.Backend.Data.Objects
{
    public class Login : BaseObject
    {
        public string UserName, HashedPassword, AccessToken,Email;
        public DateTime LastLoginDateTime;

        public static Login FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<Login>();
        }

        public static Login FromID(int ID,bool WithSecretData=false)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("ID",ID) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Logins.LoginID, Logins.UserName, Logins.HashedPassword, Logins.AccessToken, Logins.LastLoginDateTime, Logins.Email
FROM Logins
WHERE (((Logins.LoginID)=@ID));
", Params);
            return FromRData(RData, WithSecretData);
        }

        public static Login FromUserName(string UserName,bool WithSecretData=false)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("UserName", UserName) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Logins.LoginID, Logins.UserName, Logins.HashedPassword, Logins.AccessToken, Logins.LastLoginDateTime, Logins.Email
FROM Logins
WHERE (((Logins.UserName)=@UserName));
", Params);
            return FromRData(RData, WithSecretData);
        }

        public static Login FromEmail(string Email,bool WithSecretData = false)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("Email",Email) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Logins.LoginID, Logins.UserName, Logins.HashedPassword, Logins.AccessToken, Logins.LastLoginDateTime, Logins.Email
FROM Logins
WHERE (((Logins.Email)=@Email));
", Params);
            return FromRData(RData, WithSecretData);
        }

        public static Login FromAccessToken(string AccessToken)
        {
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("AccessToken", AccessToken) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Logins.LoginID, Logins.UserName, Logins.HashedPassword, Logins.AccessToken, Logins.LastLoginDateTime, Logins.Email
FROM Logins
WHERE (((Logins.AccessToken)=@AccessToken));
",Params);
            return FromRData(RData, true);
        }

        static Login FromRData(List<string[]> RData, bool WithSecretData)
        {
            if (RData.Count == 0) { return null; }
            Login Login = new Login();
            Login.ID = int.Parse(RData[0][0]);
            Login.UserName = RData[0][1];
            if (WithSecretData)
            {
                Login.HashedPassword = RData[0][2];
                Login.AccessToken = RData[0][3];
                Login.Email = RData[0][5];
            }
            Login.LastLoginDateTime = DateTime.Parse(RData[0][4]);
            return Login;
        }

        public bool Save()
        {
            if (FromEmail(this.Email) == null && FromUserName(this.UserName) == null)
            {
                List<OleDbParameter> Params = new List<OleDbParameter> {
                    new OleDbParameter("UserName",this.UserName),
                    new OleDbParameter("HashedPassword",this.HashedPassword),
                    new OleDbParameter("AccessToken",Networking.TokenSystem.CreateToken(32)),
                    new OleDbParameter("LastLoginDateTime",DateTime.Now.ToString()),
                    new OleDbParameter("Email",this.Email)
                };
                Init.SQLi.Execute(@"INSERT INTO Logins (UserName, HashedPassword, AccessToken, LastLoginDateTime, Email) VALUES (@UserName, @HashedPassword, @AccessToken, @LastLoginDateTime, @Email)", Params);
                return true;
            }
            return false;
        }

        public bool UpdateToken()
        {
            if (FromID(this.ID)!=null)
            {
                this.AccessToken = Networking.TokenSystem.CreateToken(32);
                this.LastLoginDateTime = DateTime.Now;
                List<OleDbParameter> Params = new List<OleDbParameter> {
                    new OleDbParameter("AccessToken",this.AccessToken),
                    new OleDbParameter("LastLoginDateTime",this.LastLoginDateTime.ToString()),
                    new OleDbParameter("ID",this.ID)
                };
                Init.SQLi.Execute(@"UPDATE Logins SET Logins.AccessToken = @AccessToken, Logins.LastLoginDateTime = @LastLoginDateTime
WHERE(((Logins.LoginID) = @ID));
                ", Params);
                return true;
            }
            return false;
        }
    }
}
