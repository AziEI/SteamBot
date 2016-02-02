using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SQLite;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using Csgotm;
using CsgotmBot;
using NDesk.Options;
using SteamAuth;
using SteamBot.Csgotm;
using SteamTrade;

namespace SteamBot
{
    public class Program
    {
        private static bool showHelp;

        private static int botIndex = -1;
        private static BotManager manager;
        private static bool isclosing = false;

        [STAThread]
        public static void Main(string[] args)
        {
            //SQLiteConnection m_dbConnection;
            //m_dbConnection = new SQLiteConnection("Data Source=" + CsgotmConfig.PathToSql + "; Version=3");
            ////m_dbConnection = new SQLiteConnection("Data Source=csgotmBase.sqlite; Version=3");
            //m_dbConnection.Open();


            //SteamGuardAccount sga = new SteamGuardAccount();
            //sga.SharedSecret = "";
            //Console.WriteLine(sga.GenerateSteamGuardCode());
            //Thread.Sleep(1000000);


            CsgotmConfig cf = CsgotmConfig.LoadConfig();
            CsgotmAPI.SetApiKey(cf.ApiKey);

            if (!CsgotmAPI.ApiKeyIsValid())
            {
                {
                    Console.WriteLine("API key is not valid. Probably you didn't set ApiKey in csgotmSettings file.");
                    Console.Write("Press Enter to exit...");
                    Console.ReadLine();
                    return;
                }
            }
            




            //CsgotmAPI.GetTrades();
            //Console.WriteLine("Sleeping");
            //CsgotmAPI.StartPingPong(60000);



            //SQLHelper sqlHelper = SQLHelper.getInstance();
            //ItemInSQL item = new ItemInSQL("https://market.csgo.com/item/520025252-0-Operation+Breakout+Weapon+Case/",
            //    CsgotmConfig.ApiKey, CsgotmConfig.SteamWeb, 1, 1, 50, 66);
            //ItemInSQL itemSelected = sqlHelper.Select(item);
            //Console.WriteLine(itemSelected.ItemName);
            ////sqlHelper.Add(item);
            ////CsgotmAPI.SellItem(item, 150);
            //Thread.Sleep(200000);

            //GenericInventory inventory = new GenericInventory(CsgotmConfig.SteamWeb);










            //List<ItemInSQL> items = new List<ItemInSQL>();
            //SQLHelper sqlHelper = null;
            //try
            //{
            //    sqlHelper = SQLHelper.getInstance();
            //    sqlHelper.Open();
            //    foreach (var item in items)
            //    {
            //            sqlHelper.Add(item);

            //    }
            //    CsgotmAPI.RenewPrices();
            //    sqlHelper.Close();
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.Message);
            //    if (sqlHelper != null) {
            //        sqlHelper.Close();
            //    }
            //}

            //Console.WriteLine("Sleeping");
            //Thread.Sleep(100000);
            //if (showHelp)
            //{
            //    Console.ForegroundColor = ConsoleColor.White;
            //    Console.WriteLine("If no options are given SteamBot defaults to Bot Manager mode.");
            //    Console.Write("Press Enter to exit...");
            //    Console.ReadLine();
            //    return;
            //}

            BotManagerMode();
        }

        #region SteamBot Operational Modes

        // This mode is to run a single Bot until it's terminated.
        private static void BotMode(int botIndex)
        {
            if (!File.Exists("settings.json"))
            {
                Console.WriteLine("No settings.json file found.");
                return;
            }

            Configuration configObject;
            try
            {
                configObject = Configuration.LoadConfiguration("settings.json");
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                // handle basic json formatting screwups
                Console.WriteLine("settings.json file is corrupt or improperly formatted.");
                return;
            }

            if (botIndex >= configObject.Bots.Length)
            {
                Console.WriteLine("Invalid bot index.");
                return;
            }

            Bot b = new Bot(configObject.Bots[botIndex], configObject.ApiKey, BotManager.UserHandlerCreator, true, true);
            Console.Title = "Bot Manager";
            b.StartBot();

            string AuthSet = "auth";
            string ExecCommand = "exec";
            string InputCommand = "input";

            // this loop is needed to keep the botmode console alive.
            // instead of just sleeping, this loop will handle console input
            while (true)
            {
                string inputText = Console.ReadLine();

                if (String.IsNullOrEmpty(inputText))
                    continue;

                // Small parse for console input
                var c = inputText.Trim();

                var cs = c.Split(' ');

                if (cs.Length > 1)
                {
                    if (cs[0].Equals(AuthSet, StringComparison.CurrentCultureIgnoreCase))
                    {
                        b.AuthCode = cs[1].Trim();
                    }
                    else if (cs[0].Equals(ExecCommand, StringComparison.CurrentCultureIgnoreCase))
                    {
                        b.HandleBotCommand(c.Remove(0, cs[0].Length + 1));
                    }
                    else if (cs[0].Equals(InputCommand, StringComparison.CurrentCultureIgnoreCase))
                    {
                        b.HandleInput(c.Remove(0, cs[0].Length + 1));
                    }
                }
            }
        }

        // This mode is to manage child bot processes and take use command line inputs
        private static void BotManagerMode()
        {
            Console.Title = "Bot Manager";
            
            manager = new BotManager();
            var loadedOk = manager.LoadConfiguration("settings.json");

            if (!loadedOk)
            {
                Console.WriteLine(
                    "Configuration file Does not exist or is corrupt. Please rename 'settings-template.json' to 'settings.json' and modify the settings to match your environment");
                Console.Write("Press Enter to exit...");
                Console.ReadLine();
            }
            else
            {
                if (manager.ConfigObject.UseSeparateProcesses)
                    SetConsoleCtrlHandler(ConsoleCtrlCheck, true);

                if (manager.ConfigObject.AutoStartAllBots)
                {
                    var startedOk = manager.StartBots();
                    if (!startedOk)
                    {
                        Console.WriteLine(
                            "Error starting the bots because either the configuration was bad or because the log file was not opened.");
                        Console.Write("Press Enter to exit...");
                        Console.ReadLine();
                    }
                }
                else
                {
                    foreach (var botInfo in manager.ConfigObject.Bots)
                    {
                        if (botInfo.AutoStart)
                        {
                            // auto start this particual bot...
                            manager.StartBot(botInfo.Username);
                        }
                    }
                }
                Console.WriteLine("Type help for bot manager commands. ");
                Console.Write("botmgr > ");

                var bmi = new BotManagerInterpreter(manager);

                // command interpreter loop.
                do
                {
                    string inputText = Console.ReadLine();

                    if (String.IsNullOrEmpty(inputText))
                        continue;

                    bmi.CommandInterpreter(inputText);

                    Console.Write("botmgr > ");

                } while (!isclosing);
            }
        }

        #endregion Bot Modes

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            // Put your own handler here
            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                case CtrlTypes.CTRL_BREAK_EVENT:
                case CtrlTypes.CTRL_CLOSE_EVENT:
                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    if (manager != null)
                    {
                        manager.StopBots();
                    }
                    isclosing = true;
                    break;
            }
            
            return true;
        }


        #region Console Control Handler Imports

        // Declare the SetConsoleCtrlHandler function
        // as external and receiving a delegate.
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        #endregion
    }
}
