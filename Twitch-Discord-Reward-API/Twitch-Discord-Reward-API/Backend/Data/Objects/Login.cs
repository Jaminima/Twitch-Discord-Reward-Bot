﻿using System;
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
            if (UserName == null) { return null; }
            List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("UserName", UserName) };
            List<String[]> RData = Init.SQLi.ExecuteReader(@"SELECT Logins.LoginID, Logins.UserName, Logins.HashedPassword, Logins.AccessToken, Logins.LastLoginDateTime, Logins.Email
FROM Logins
WHERE (((Logins.UserName)=@UserName));
", Params);
            return FromRData(RData, WithSecretData);
        }

        public static Login FromEmail(string Email,bool WithSecretData = false)
        {
            if (Email == null) { return null; }
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

        public bool Save()
        {
            if (FromEmail(this.Email) == null && FromUserName(this.UserName) == null)
            {
                List<OleDbParameter> Params = new List<OleDbParameter> {
                    new OleDbParameter("HashedPassword",this.HashedPassword),
                    new OleDbParameter("AccessToken",Init.ScryptEncoder.Encode(Networking.TokenSystem.CreateToken(64))),
                    new OleDbParameter("LastLoginDateTime",DateTime.Now.ToString())
                };
                string PreValue = ""; string PostValue = "";
                if (this.Email != null) { Params.Add(new OleDbParameter("Email", this.Email)); PreValue += "Email"; PostValue += "@Email"; }
                if (this.UserName != null) {
                    Params.Add(new OleDbParameter("UserName", this.UserName));
                    if (PreValue != "") { PreValue += ","; PostValue += ","; }
                    PreValue += "UserName"; PostValue += "@UserName";
                }
                Init.SQLi.Execute(@"INSERT INTO Logins (HashedPassword, AccessToken, LastLoginDateTime, "+PreValue+@") VALUES (@HashedPassword, @AccessToken, @LastLoginDateTime, "+PostValue+@")", Params);
                return true;
            }
            return false;
        }

        public bool UpdateToken()
        {
            if (FromID(this.ID)!=null)
            {
                this.AccessToken = Networking.TokenSystem.CreateToken(64);
                this.LastLoginDateTime = DateTime.Now;
                List<OleDbParameter> Params = new List<OleDbParameter> {
                    new OleDbParameter("AccessToken",Init.ScryptEncoder.Encode(this.AccessToken)),
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

        public bool UpdateUserNameEmailPassword()
        {
            bool IsTaken = false;
            if (FromUserName(this.UserName) != null) { if (FromUserName(this.UserName).ID != this.ID) { IsTaken = true; } }
            if (FromEmail(this.Email) != null) { if (FromEmail(this.Email).ID != this.ID) { IsTaken = true; } }
            if (FromID(this.ID) != null)
            {
                if (!IsTaken)
                {
                    List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("HashedPassword",this.HashedPassword) };
                    string UpdateString = "";
                    if (this.UserName != null) { UpdateString += ", Logins.UserName = @UserName"; Params.Add(new OleDbParameter("UserName", this.UserName)); }
                    if (this.Email != null) { UpdateString += ", Logins.Email = @Email"; Params.Add(new OleDbParameter("Email", this.Email)); }
                    Params.Add(new OleDbParameter("ID", this.ID));
                    Init.SQLi.Execute(@"UPDATE Logins SET Logins.HashedPassword = @HashedPassword"+UpdateString+@"
WHERE(((Logins.LoginID) = @ID));
", Params);
                    return true;
                }
            }
            return false;
        }

        public void Delete()
        {
            if (FromID(this.ID) != null)
            {
                foreach (Currency C in Currency.FromLogin(this.ID)) { C.Delete(); }//Delete all currencies tied to this login
                List<OleDbParameter> Params = new List<OleDbParameter> { new OleDbParameter("ID", this.ID) };
                Init.SQLi.Execute(@"DELETE FROM Logins
WHERE (((Logins.LoginID)=@ID));
", Params);
            }
        }
    }
}
