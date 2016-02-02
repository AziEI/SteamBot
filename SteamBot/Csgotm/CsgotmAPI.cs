using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CsgotmBot;
using Newtonsoft.Json;
using SteamBot;
using SteamBot.Csgotm;
using SteamBot.TF2GC;
using SteamTrade;

namespace Csgotm
{
    class CsgotmAPI
    {
        static object renewPricesLocker = new object();
        private static string ApiKey;

        //public CsgotmAPI(string apiKey)
        //{
        //    ApiKey = apiKey;
        //}
        public static void SetApiKey(string apiKey)
        {
            ApiKey = apiKey;
        }

        public static Boolean SetAutoBuy(ItemInSQL itemInSql, string apiKey, SteamTrade.SteamWeb steamWeb)
        {
            String deserializedResponse;
            string lang = "en"; //needed for some requests
            //TODO I'm inserting a new AutoBuyPrice, but can't change.
            //calculating new price for ItemInSQL
            int currentAutoBuyPrice = GetMaxAutoBuyPriceForItem(itemInSql, lang, apiKey, steamWeb); //100
            int newPrice;
            if (currentAutoBuyPrice < itemInSql.MinPriceToBuy)
            {
                newPrice = itemInSql.MinPriceToBuy;
            }
            else if (currentAutoBuyPrice < itemInSql.MaxPriceToBuy)
            {
                newPrice = currentAutoBuyPrice + 1;
            }
            else
            {
                newPrice = itemInSql.MaxPriceToBuy;
            }
            //trying to update price for ItemInSQL
            string updateLink = string.Format("https://csgo.tm/api/UpdateOrder/{0}/{1}/{2}/?key={3}",
                itemInSql.GetItemClass(), itemInSql.GetItemInstance(), newPrice, apiKey);

            string updateJsonResponse = steamWeb.Fetch(updateLink,"GET", null, false, null);
            deserializedResponse = JsonConvert.DeserializeObject(updateJsonResponse).ToString();
            //if couldn't find the order in base - create a new one.
            if (deserializedResponse.ToString().Contains("Данная заявка на покупку не найдена"))
            {
                string insertLink = string.Format("https://csgo.tm/api/InsertOrder/{0}/{1}/{2}/{3}/?key={4}",
                itemInSql.GetItemClass(), itemInSql.GetItemInstance(), newPrice, itemInSql.Hash, apiKey);
                string insertJsonResponse = steamWeb.Fetch(insertLink, "GET", null, false, null);
                deserializedResponse = JsonConvert.DeserializeObject(insertJsonResponse).ToString();
                if (deserializedResponse.Contains("true"))
                {
                    Console.WriteLine("A new order for ItemInSQL " + itemInSql.ItemName + " was created successfully!" +
                                      " Price: " + (double)newPrice/100 + " руб.");
                    return true;
                }
                Console.WriteLine("For item: " + itemInSql.ItemName);
                Console.WriteLine(deserializedResponse);
            }
            else if (deserializedResponse.ToString().Contains("недостаточно средств на счету"))
            {
                Console.WriteLine("ItemInSQL " + itemInSql.ItemName + " - not enough funds in wallet! Пополните кошелек!");
                return false;
            }
            else if (deserializedResponse.ToString().Contains("true"))
            {
                Console.WriteLine("ItemInSQL " + itemInSql.ItemName + " was updated successfully! New price: " + (double)newPrice/100 + " руб.");
                return true;
            }
            //TODO
            Console.WriteLine("We should never reach here." +
                              " A new error ");
            return false;
        }

        public static int GetMaxAutoBuyPriceForItem(ItemInSQL itemInSql, string lang, string apikey, SteamTrade.SteamWeb steamWeb)
        {
            //https://csgo.tm/api/ItemInfo/520025252_0/en/?key=  //in json properties get autoBuyOffers
            string requestLink = string.Format("https://csgo.tm/api/ItemInfo/{0}/{1}/?key={2}", itemInSql.ClassInstance, lang, apikey);
            string jsonItemInfo = steamWeb.Fetch(requestLink, "GET", null, false, null);
            dynamic deserializedResponse = JsonConvert.DeserializeObject(jsonItemInfo);
            int price = deserializedResponse.buy_offers[0].o_price;
            return price;
        }

        public static string GetItemInfo(string class_instance, string lang, string apikey, SteamTrade.SteamWeb steamWeb)
        {
            string requestLink = string.Format("https://csgo.tm/api/ItemInfo/{0}/{1}/?key={2}", class_instance, lang, apikey);
            string response = steamWeb.Fetch(requestLink, "GET", null, false, null);
            //if (response.Contains("error"))
            //{
            //    throw new CsgotmException(response);
            //}
            return response;
        }

        public static void StartRenewPricesToAutoBuy(int delay)
        {
            System.Timers.Timer renewPricesTimer = new System.Timers.Timer(delay);
            renewPricesTimer.Elapsed += RenewPrices;
            renewPricesTimer.AutoReset = true;
            renewPricesTimer.Enabled = true;
            Console.WriteLine("Renew prices started");
        }
        private static void RenewPrices(Object source, ElapsedEventArgs e) 
        {
            if (Monitor.TryEnter(renewPricesLocker))
            {
                try
                {
                    Console.WriteLine("Renewing prices...");
                    SQLHelper sqlHelper = SQLHelper.getInstance();
                    List<ItemInSQL> items = sqlHelper.SelectAll();
                    foreach (var item in items)
                    {
                        if (item.MaxToBuy > item.Bought)
                        {
                            SetAutoBuy(item, ApiKey, CsgotmConfig.SteamWeb);
                        }
                        else
                        {
                            Console.WriteLine("ItemInSQL " + item.ItemName +
                                              " has reached maximum buys. Increase MaxToBuy.");
                        }
                    }
                    Console.WriteLine("Prices renewed successfully!");
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    Monitor.Exit(renewPricesLocker);
                }
            }
        }

        public static List<ItemOnCsgotm> GetTrades()
        {
            List<ItemOnCsgotm> itemsOnCsgotm = new List<ItemOnCsgotm>();
            string gettradesLink = string.Format("https://csgo.tm/api/Trades/?key={0}", ApiKey);
            string jsonTrades = CsgotmConfig.SteamWeb.Fetch(gettradesLink, "GET", null, false, null);
            if (jsonTrades.Contains("error")) {
                throw new CsgotmException("error encountered " + jsonTrades);
            }
            else if (jsonTrades.Contains("[]"))
            {
                //TODO return null items
            }
            else
            {
                dynamic deserializedResponse = JsonConvert.DeserializeObject(jsonTrades);
                foreach (var itemOnCsgotm in deserializedResponse)
                {
                    var itemId = int.Parse(itemOnCsgotm.ui_id.ToString());
                    var itemStatus = int.Parse(itemOnCsgotm.ui_status.ToString());
                    var botId = int.Parse(itemOnCsgotm.ui_bid.ToString());
                    itemsOnCsgotm.Add(new ItemOnCsgotm(itemId, itemStatus,
                        botId));
                }
                //TODO return what I need in trades.
                //для изменения цены выставленного на продажу предмета нужно знать ui_id 
                //для передачи вещей - хз, мб. ui_id
                //для вывода вещей мне нужно забирать отсюда только список botid.
            }
            return itemsOnCsgotm;
        }
        public static void StartPingPong(int delay)
        {
            //Perform it once in order to not wait for delay.
            Console.WriteLine("PingPong started.");
            System.Timers.Timer pingPongTimer = new System.Timers.Timer(delay);
            pingPongTimer.Elapsed += OnTimedEvent;
            pingPongTimer.AutoReset = true;
            pingPongTimer.Enabled = true;
        }
        private static bool PingPong()
        {
            if (!string.IsNullOrEmpty(ApiKey))
            {
                string pingpongLink = string.Format("https://csgo.tm/api/PingPong/?key={0}", ApiKey);
                string response = CsgotmConfig.SteamWeb.Fetch(pingpongLink, "GET", null, false, null);
                if (response.Contains("true"))
                {
                    Console.WriteLine("PingPong true");
                    return true;
                }
                else
                {
                    Console.WriteLine("PingPong - FALSE");
                    Console.WriteLine(response);
                    return false;
                }
            }
            Console.WriteLine("Set API Key!");
            return false;
        }
        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
                PingPong();
        }
        public static string ItemRequest(Boolean sendingItems, string botid)
        {
            string inOurOut;
            if (sendingItems)
            {
                inOurOut = "in";
            }
            else
            {
                inOurOut = "out";
            }
            string itemRequestLink = String.Format("https://csgo.tm/api/ItemRequest/{0}/{1}/?key={2}",
                inOurOut, botid, ApiKey);
            string response = CsgotmConfig.SteamWeb.Fetch(itemRequestLink, "GET", null, false, null);
            return response;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="price">price in kopeck(копейка)</param>
        public static void SellItem(ItemInSQL item, int price)
        {
            string itemInfo = GetItemInfo(item.ClassInstance, "en", ApiKey, CsgotmConfig.SteamWeb);
            if (!itemInfo.Contains("error"))
            {
                dynamic itemInfoJson = JsonConvert.DeserializeObject(itemInfo);
                var currentLowestPrice = itemInfoJson.offers[0].price;
                Console.WriteLine("Current price: " + (double)currentLowestPrice/100 + " .руб");
                if (currentLowestPrice > 100)
                {
                    if (currentLowestPrice > price)
                    {
                        price = currentLowestPrice - 1;
                    }
                }
                else
                {
                    price = 100;
                }
                string sellItemLink = String.Format("https://csgo.tm/api/SetPrice/new_{0}/{1}/?key={2}",
                        item.ClassInstance, price, ApiKey);
                string sellItemResponse = CsgotmConfig.SteamWeb.Fetch(sellItemLink, "GET", null, false, null);
                if (sellItemResponse.Contains("\"result\":1"))
                {
                    Console.WriteLine(item.ItemName + " is selling!");
                }
                else
                {
                    Console.WriteLine("Error while setting item to sell on website: " + sellItemResponse);
                }
            }
        }

        public static bool ApiKeyIsValid()
        {
            //TODO probably do it in more neat way?
            string checkString = string.Format("https://csgo.tm/api/InventoryStatus/?key={0}", ApiKey);
            string jsonResponse = CsgotmConfig.SteamWeb.Fetch(checkString, "GET", null, false, null);
            if (jsonResponse.Contains("true"))
            {
                return true;
            }
            return false;
        }

    }

    class ItemOnCsgotm
    {
        public int itemId { get; private set; }
        public ItemOnCsgotmStatus status { get; private set; }
        public int botId { get; private set; }

        public ItemOnCsgotm(int item_id, int status, int botId)
        {
            this.itemId = item_id;
            //this.status = status;
            switch (status)
            {
                case 1:
                    this.status = ItemOnCsgotmStatus.Selling;
                break;
                case 2:
                    this.status = ItemOnCsgotmStatus.SendToCsgotmbot;
                    break;
                case 3:
                    this.status = ItemOnCsgotmStatus.Bought;
                    break;
                case 4:
                    this.status = ItemOnCsgotmStatus.ToReceiveFromCsgotmbot;
                    break;
            }
            this.botId = botId;
        }
    }

    //enum CsgotmErrors
    //{
    //    NoKey,
    //    BadKey,
    //    UnexpectedError
    //}
    enum ItemOnCsgotmStatus
    {
        Selling,
        SendToCsgotmbot,
        Bought,
        ToReceiveFromCsgotmbot
    }
}
