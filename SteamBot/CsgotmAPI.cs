using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamBot.Csgotm;

namespace Csgotm
{
    class CsgotmAPI
    {
        public static Boolean SetAutoBuy(Item item, string apiKey, SteamTrade.SteamWeb steamWeb)
        {
            string lang = "en"; //needed for some requests
            string class_instance = item.ClassInstance;
            int price = 99;
            string hash = "147d375acdcae0645152b224d34d66ee";
            //TODO check the current maxAutoBuyPrice on csgo.tm
            MaxAutoBuyPrice(class_instance, lang, apiKey, steamWeb);

            //https://csgo.tm/api/InsertOrder/[class]/[instance]/[price]/[hash]/?key=[your_secret_key] //setAutoBuyPrice
            //string requestLink = string.Format("https://csgo.tm/api/InsertOrder/{0}/{1}/{2}/{3}/?key={4}",
            //    item.GetItemClass(), item.GetItemInstance(),
            //    price, hash, apikey);
            return true;
        }

        public static int MaxAutoBuyPrice(string class_instance, string lang, string apikey, SteamTrade.SteamWeb steamWeb)
        {
            //https://csgo.tm/api/ItemInfo/520025252_0/en/?key=dzrux9i3SpWyQS5eXZcoBSrN5P5BSvy  //in json properties get autoBuyOffers
            string requestLink = string.Format("https://csgo.tm/api/ItemInfo/{0}/{1}/?key={2}", class_instance, lang, apikey);
            dynamic jsonItemInfo = steamWeb.Fetch(requestLink, "GET", null, false, null);
            var price = jsonItemInfo.buy_offers[0].o_price;
            return 0;
        }

        public static string GetItemInfo(string class_instance, string lang, string apikey, SteamTrade.SteamWeb steamWeb)
        {
            string requestLink = string.Format("https://csgo.tm/api/ItemInfo/{0}/{1}/?key={2}", class_instance, lang, apikey);
            string response = steamWeb.Fetch(requestLink, "GET", null, false, null);
            if (response.Contains("error"))
            {
                throw new CsgotmException(response);
            }
            return response;
        }
    }
}
