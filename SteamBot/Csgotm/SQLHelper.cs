using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Csgotm;
using SteamBot;

namespace CsgotmBot
{
    //class for handling all actions with sql
    class SQLHelper
    {
        private List<string> allowedParameters = new List<string>();
        private static SQLHelper instance = null;
        private string ConnectionString = "";

        //SQLiteConnection m_dbConnection;
        //Constructor creates SQLConnection, but doesn't open it.
        //public SQLHelper()
        //{
        //    if (m_dbConnection == null)
        //    {
        //        SqlHelperConstructor();
        //    }
        //}
        public static SQLHelper getInstance()
        {
            if (instance == null)
            {
                instance = new SQLHelper();
            }
            return instance;
        }
        private SQLHelper()
        {
            var directory = Directory.GetCurrentDirectory();
            var sqlPath = directory + "\\SQL_Base\\csgotmBase.sqlite";
            ConnectionString = "Data Source=" + sqlPath + "; Version=3;Pooling=True;Max Pool Size=100;";
            //m_dbConnection.Open();
            //allowedParameters.Add("ItemName");
            //allowedParameters.Add("CanBuy");
            //allowedParameters.Add("MaxToBuy");
            //allowedParameters.Add("Bought");
            //allowedParameters.Add("MinPriceBuy");
            //allowedParameters.Add("MaxPriceBuy");
        }

        //public void Open()
        //{
        //    try
        //    {
        //        m_dbConnection.Open();
        //    }
        //    catch (Exception e)
        //    {
        //        Console.Write(e.Message);
        //    }
        //}
        /// <summary>
        /// ItemInSQL - updating ItemInSQL
        /// parameters - NameOfParameter - string value
        /// </summary>
        /// <param name="itemInSql"></param>
        /// <param name="parameters"></param>
        public void Update(ItemInSQL itemInSql, Dictionary<string, string> parameters)
        {
            int canBuy = 1;
            int MaxToBuy = 2;
            int Bought = 3;
            int MinPriceBuy = 4;
            int MaxPriceBuy = 5;

            allowedParameters.Add("ItemName");
            allowedParameters.Add("CanBuy");
            allowedParameters.Add("MaxToBuy");
            allowedParameters.Add("Bought");
            allowedParameters.Add("MinPriceBuy");
            allowedParameters.Add("MaxPriceBuy");
            allowedParameters.Add("MinPriceSell");
            allowedParameters.Add("MaxPriceSell");

            bool first = true;
            bool last = false;
            String updateString = "UPDATE Items SET ";

            using (SQLiteConnection connection = GetSqLiteConnection(ConnectionString))
            {
                foreach (var param in parameters)
                {
                    //checking if the updating field exists. Not neat way to do it.
                    if (allowedParameters.Contains(param.Key))
                    {
                        if (!first)
                        {
                            updateString += ", ";
                        }
                        updateString += param.Key + " = " + param.Value;
                    }
                    first = false;
                }
                updateString += " WHERE ItemID = '" + itemInSql.ClassInstance + "' AND Hash = '" + itemInSql.Hash + "'";
                SQLiteCommand command = new SQLiteCommand(updateString, connection);
                command.ExecuteNonQuery();
                Console.WriteLine("ItemInSQL " + itemInSql.ItemName + " has updated succefully!");
            }
        }
        //public void Close()
        //{
        //    if (IsOpen())
        //    {
        //        m_dbConnection.Close();
        //    }
        //}

        //private string GetConnectionString()
        //{
        //    return ConnectionString;
        //}

        private SQLiteConnection GetSqLiteConnection(string connectionString)
        {
            SQLiteConnection connection = new SQLiteConnection(connectionString);
            connection.Open();
            return connection;
        }
        public void Delete(ItemInSQL itemInSql)
        {
            using (SQLiteConnection connection = GetSqLiteConnection(ConnectionString))
            {
                var deleteString = "DELETE FROM items WHERE ItemID = '" +
                                   itemInSql.ClassInstance + "' AND Hash = '" + itemInSql.Hash + "'";
                if (Select(itemInSql) != null)
                {
                    var command = new SQLiteCommand(deleteString, connection);
                    command.ExecuteNonQuery();
                    Console.WriteLine("ItemInSQL " + itemInSql.ItemName + " was deleted successfully!");
                }
                else
                {
                    Console.WriteLine("ItemInSQL " + itemInSql.ItemName + " was not found in base!");
                }
            }
        }
        //selecting items via class_instance
        public ItemInSQL Select(ItemInSQL item)
        {
            return SelectWithClassInstance(item.ClassInstance);
        }

        public ItemInSQL Select(string class_instance)
        {
            return SelectWithClassInstance(class_instance);
        }
        private ItemInSQL SelectWithClassInstance(string class_instance)
        {
            using (SQLiteConnection connection = GetSqLiteConnection(ConnectionString))
            {
                var sqlSelect = "SELECT * FROM Items WHERE ItemID = '" + class_instance +
                                "'";
                var command = new SQLiteCommand(sqlSelect, connection);
                var reader = command.ExecuteReader();
                var items = new List<ItemInSQL>();
                while (reader.Read())
                {
                    var itemInSql = new ItemInSQL(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2),
                        reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
                        reader.GetInt32(7), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10),
                        reader.GetInt32(11), reader.GetInt32(12), reader.GetInt32(13), reader.GetInt32(14));
                    items.Add(itemInSql);
                }
                if (items.Count == 0)
                {
                    Console.WriteLine("No items was found for class_instance " + class_instance);
                    return null;
                }
                if (items.Count == 1)
                {
                    return items[0];
                }
                //TODO: if I change console to log - change here.
                //if code reaches here, we have 2+ items, returning the firest one and notificationg about that.
                Console.WriteLine("For class-instance " + class_instance + " we have more than one row in db.");
                Console.WriteLine("Row numbers of items: ");
                foreach (var collisionItem in items)
                {
                    Console.WriteLine(collisionItem._n);
                }
                Console.WriteLine("Returning first ItemInSQL with number " + items[0]._n);
                return items[0];
            }
        }
        //TODO this method mostly reapeats "Select" method. Rewrite so it'll not just repeat same code twice.
        public List<ItemInSQL> SelectAll()
        {
            using (SQLiteConnection connection = GetSqLiteConnection(ConnectionString))
            {
                List<ItemInSQL> items = new List<ItemInSQL>();
                var sqlSelect = "SELECT * FROM Items";
                var command = new SQLiteCommand(sqlSelect, connection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var itemInSql = new ItemInSQL(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2),
                        reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
                        reader.GetInt32(7), reader.GetInt32(8), reader.GetInt32(9), reader.GetInt32(10),
                        reader.GetInt32(11), reader.GetInt32(12), reader.GetInt32(13), reader.GetInt32(14));
                    items.Add(itemInSql);
                }
                return items;
            }
        }
        //public bool CheckUnique(String class_instance)
        //{
            
        //}
        
        
        public void Add(ItemInSQL itemInSql)
        {
            using (SQLiteConnection connection = GetSqLiteConnection(ConnectionString))
            {
                if (Select(itemInSql) == null)
                {
                    var insertString = "INSERT INTO Items " +
                                       "('Context','ItemID','ItemName','ItemURL','ItemURLSteam','Hash','CanBuy'" +
                                       ",'CanSell','MaxToBuy','Bought','MinPriceBuy','MaxPriceBuy', 'MinPriceSell', 'MaxPriceSell')" +
                                       "VALUES (@context,@itemID,@itemName,@itemURL,@itemURLSteam,@hash,@canBuy," +
                                       "@canSell,@maxToBuy,@bought,@minPriceBuy,@maxPriceBuy,@minPriceSell,@maxPriceSell);";
                    var command = new SQLiteCommand(insertString, connection);
                    command.Parameters.AddWithValue("@context", itemInSql.Context);
                    //number for game - csgo = 730, tf2 = 440, dota2 = 570, steam = 753
                    command.Parameters.AddWithValue("@itemID", itemInSql.ClassInstance);
                    command.Parameters.AddWithValue("@itemName", itemInSql.ItemName);
                    command.Parameters.AddWithValue("@itemURL", itemInSql.ItemUrl);
                    command.Parameters.AddWithValue("@itemURLSteam", itemInSql.ItemUrlSteam);
                    command.Parameters.AddWithValue("@hash", itemInSql.Hash);
                    command.Parameters.AddWithValue("@canBuy", itemInSql.CanBuy);
                    command.Parameters.AddWithValue("@canSell", itemInSql.CanSell);
                    command.Parameters.AddWithValue("@maxToBuy", itemInSql.MaxToBuy);
                    command.Parameters.AddWithValue("@bought", itemInSql.Bought);
                    command.Parameters.AddWithValue("@minPriceBuy", itemInSql.MinPriceBuy);
                    command.Parameters.AddWithValue("@maxPriceBuy", itemInSql.MaxPriceBuy);
                    command.Parameters.AddWithValue("@minPriceSell", itemInSql.MinPriceSell);
                    command.Parameters.AddWithValue("@maxPriceSell", itemInSql.MaxPriceSell);
                    command.ExecuteNonQuery();
                    Console.WriteLine("ItemInSQL " + itemInSql.ItemName + " was added successfully!");
                }
                else
                {
                    Console.WriteLine("The ItemInSQL " + itemInSql.ItemName + " already in base");
                }
            }
        }

        //private bool IsOpen()
        //{
        //    if (m_dbConnection.State == ConnectionState.Open) return true;
        //    return false;
        //}
        
    }
}
