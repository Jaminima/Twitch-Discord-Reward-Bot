using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;

namespace Twitch_Discord_Reward_API.Backend.Data
{
    public class SQL
    {
        private OleDbConnection Conn;//Stores The Active Database Coneection
        private string DBase = "";//Stores The Databases File Path
        public SQL(string DataBase)
        {
            DBase = DataBase;
            RestartConn();
        }

        private void RestartConn()
        {
            if (Conn != null) { if (Conn.State == System.Data.ConnectionState.Open) { Conn.Close(); } /* If connection is open, close it*/ }
            Conn = new OleDbConnection("Provider = Microsoft.ACE.OLEDB.12.0; Data Source = " + DBase + ".accdb"); // Open a new database connection
            Conn.Open();
        }

        public List<String[]> ExecuteReader(String sCommand, List<OleDbParameter> ParamCollection = null)
        {
            OleDbCommand Command = new OleDbCommand(sCommand, Conn); // Create the command, using the opened connection and the sql string
            if (ParamCollection != null) { for (int i = 0; i < ParamCollection.Count; i++) { Command.Parameters.Add(ParamCollection[i]); } } // Add the paramaters
            OleDbDataReader Results = Command.ExecuteReader(); // Execute the reader and store the result
            List<String[]> LResults = new List<string[]> { }; // Create a list of String[] too store the rows and collumns of the results
            while (Results.Read()) // Keep reading untill all is read
            {
                string[] Data = new string[Results.FieldCount]; // Create a temporary String[]
                for (int i = 0; i < Results.FieldCount; i++) { Data[i] = Results.GetValue(i).ToString(); } // Place each collumn in the row into the array
                LResults.Add(Data); // Add the row to the list
            }
            Results.Close(); // Terminate read and pass the formatted results back
            return LResults;
        }

        public void Execute(String sCommand, List<OleDbParameter> ParamCollection = null)
        {
            OleDbCommand Command = new OleDbCommand(sCommand, Conn); // Create the command, using the opened connection ad the sql string parameter
            if (ParamCollection != null) { for (int i = 0; i < ParamCollection.Count; i++) { Command.Parameters.Add(ParamCollection[i]); } } // Add the paramaters
            try { Command.ExecuteNonQuery(); /*RestartConn();*/ } catch (OleDbException E) { Console.WriteLine(E); } // Execute the command
        }
    }
}
