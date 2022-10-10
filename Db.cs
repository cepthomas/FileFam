using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data.SQLite;
using NBagOfTricks.Slog;

namespace Notr
{
    //SQLite database files are commonly used as containers to transfer rich content between systems and as a long-term archival
    //format for data.SQLite uses dynamic types for tables.It means you can store any value in any column, regardless of
    //the data type.

    //How do I make SQLite persistent?
    //Establish connection with the database using connect() function and obtain connection object.
    //Call cursor() method of connection object to get cursor object.
    //Form a query string made up of a SQL statement to be executed.
    //Execute the desired query by invoking execute() method.
    //Close the connection.

    // you're only requiring one IO action (on the commit )

    /// <summary>
    /// Just test/dev stuff right now.
    /// </summary>
    public class Db
    {
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("DbTest");

        readonly string _cs = @"URI=file:C:\Dev\repos\Notr\Test\test.db";


        public void DoAll()
        {
            Version();
            CreateTable();
            InsertRow();
            ReadRows();
            ReadColumns();
        }

        public void Version()
        {
            using var con = new SQLiteConnection("Data Source=:memory:");
            con.Open();

            using var cmd = new SQLiteCommand("SELECT SQLITE_VERSION()", con);
            string? version = cmd.ExecuteScalar().ToString();

            _logger.Info($"SQLite version: {version}");
        }

        public void CreateTable()
        {
            using var con = new SQLiteConnection(_cs);
            con.Open();

            using var cmd = new SQLiteCommand(con);

            cmd.CommandText = "DROP TABLE IF EXISTS cars";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"CREATE TABLE cars(id INTEGER PRIMARY KEY,
            name TEXT, price INT)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO cars(name, price) VALUES('Audi', 52642)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO cars(name, price) VALUES('Mercedes', 57127)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO cars(name, price) VALUES('Skoda', 9000)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO cars(name, price) VALUES('Volvo', 29000)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO cars(name, price) VALUES('Bentley', 350000)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO cars(name, price) VALUES('Citroen', 21000)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO cars(name, price) VALUES('Hummer', 41400)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO cars(name, price) VALUES('Volkswagen', 21600)";
            cmd.ExecuteNonQuery();

            _logger.Info("Table cars created");
        }

        public void InsertRow()
        {
            using var con = new SQLiteConnection(_cs);
            con.Open();

            using var cmd = new SQLiteCommand(con);
            cmd.CommandText = "INSERT INTO cars(name, price) VALUES(@name, @price)";

            cmd.Parameters.AddWithValue("@name", "BMW");
            cmd.Parameters.AddWithValue("@price", 36600);
            cmd.Prepare();

            cmd.ExecuteNonQuery();

            _logger.Info("row inserted");
        }

        public void ReadRows()
        {
            using var con = new SQLiteConnection(_cs);
            con.Open();

            string stm = "SELECT * FROM cars LIMIT 5";

            using var cmd = new SQLiteCommand(stm, con);
            using SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                _logger.Info($"{rdr.GetInt32(0)} {rdr.GetString(1)} {rdr.GetInt32(2)}");
            }
        }

        public void ReadColumns()
        {
            using var con = new SQLiteConnection(_cs);
            con.Open();

            string stm = "SELECT * FROM cars LIMIT 5";

            using var cmd = new SQLiteCommand(stm, con);

            using SQLiteDataReader rdr = cmd.ExecuteReader();
            _logger.Info($"{rdr.GetName(0), -3} {rdr.GetName(1), -8} {rdr.GetName(2), 8}");

            while (rdr.Read())
            {
                _logger.Info($@"{rdr.GetInt32(0), -3} {rdr.GetString(1), -8} {rdr.GetInt32(2), 8}");
            }
        }
    }
}
