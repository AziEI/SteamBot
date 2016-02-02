using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamKit2.GC.Dota.Internal;
using SteamTrade;

namespace SteamBot.Csgotm
{
    class CsgotmConfig
    {
        //TODO save config to file and load at the start. For now it'll be manually setted constants.
        public string ApiKey;
        public static string PathToSql = Directory.GetCurrentDirectory()
            + "\\SQL_Base\\csgotmBase.sqlite";
        public static string PathToSettings = Directory.GetCurrentDirectory() + "\\csgotmSettings.json";
        public static SteamWeb SteamWeb = new SteamWeb();
        /// <summary>
        /// this method, for now, loads static ApiKey
        /// </summary>
        /// <returns></returns>
        public static CsgotmConfig LoadConfig()
        {
            try
            {
                using (StreamReader sr = new StreamReader(PathToSettings))
                {
                    string jsonConfig = sr.ReadToEnd();
                    CsgotmConfig config = JsonConvert.DeserializeObject<CsgotmConfig>(jsonConfig);
                    return config;
                }
            }
            catch (Exception e)
            {                
                Console.WriteLine(e.Message);
                return null;
            }
        }

        //public static Boolean SaveConfig(string apiKey)
        //{
        //    try
        //    {
        //        using (StreamWriter sw = File.CreateText(PathToSettings))
        //        {
        //            CsgotmConfig csgotmConfig = new CsgotmConfig(apiKey);
        //            string jsonToWrite = JsonConvert.SerializeObject(csgotmConfig);
        //            sw.Write(jsonToWrite);
        //            return true;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //        return false;
        //    }
        //}
    }
}
