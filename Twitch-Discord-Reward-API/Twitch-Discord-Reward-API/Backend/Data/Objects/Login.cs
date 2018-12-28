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
    }
}
