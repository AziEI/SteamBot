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
using SteamKit2.GC.Internal;
using SteamTrade;

namespace Csgotm
{
    class CsgotmAPI
    {
        static readonly object RenewAutobuyPricesLocker = new object();
        static readonly object PingPongLocker = new object();
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
            if (itemInSql.CanBuy == 1)
            {
                String deserializedResponse;
                string lang = "en"; //needed for some requests
                //TODO I'm inserting a new AutoBuyPrice, but can't change.
                //calculating new price for ItemInSQL
                int currentMaxAutoBuyPrice = GetMaxAutoBuyPriceForItem(itemInSql, lang, apiKey, steamWeb); //100
                int newPrice;
                if (currentMaxAutoBuyPrice < itemInSql.MinPriceBuy)
                {
                    newPrice = itemInSql.MinPriceBuy;
                }
                else if (currentMaxAutoBuyPrice < itemInSql.MaxPriceBuy)
                {
                    newPrice = currentMaxAutoBuyPrice + 1;
                }
                else
                {
                    newPrice = itemInSql.MaxPriceBuy;
                }
                //trying to update price for ItemInSQL
                string updateLink = string.Format("https://csgo.tm/api/UpdateOrder/{0}/{1}/{2}/?key={3}",
                    itemInSql.GetItemClass(), itemInSql.GetItemInstance(), newPrice, apiKey);

                string updateJsonResponse = steamWeb.Fetch(updateLink, "GET", null, false, null);
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
                        Console.WriteLine("A new autobuy order for ItemInSQL " + itemInSql.ItemName +
                                          " was created successfully!" +
                                          " Price: " + (double) newPrice/100 + " руб.");
                        return true;
                    }
                    else if (deserializedResponse.Contains("Неверно задана цена покупки"))
                    {
                        Console.WriteLine("Wrong price for " +itemInSql.ItemName + "! Check the prices for buying in database!");
                        return false;
                    }
                    Console.WriteLine("For item: " + itemInSql.ItemName);
                    Console.WriteLine(deserializedResponse);
                }
                else if (deserializedResponse.ToString().Contains("недостаточно средств на счету"))
                {
                    Console.WriteLine("Autobuy false. For " + itemInSql.ItemName +
                                      " - not enough funds in wallet! Пополните кошелек!");
                    return false;
                }
                else if (deserializedResponse.ToString().Contains("true"))
                {
                    Console.WriteLine("Autobuy price for " + itemInSql.ItemName + " was updated successfully! New price: " +
                                      (double) newPrice/100 + " руб.");
                    return true;
                }
                //TODO
                Console.WriteLine("We should never reach here." +
                                  " A new error ");
            }
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

        public static string GetItemInfo(string class_instance, string lang, SteamTrade.SteamWeb steamWeb)
        {
            string requestLink = string.Format("https://csgo.tm/api/ItemInfo/{0}/{1}/?key={2}", class_instance, lang, ApiKey);
            string response = steamWeb.Fetch(requestLink, "GET", null, false, null);
            //if (response.Contains("error"))
            //{
            //    throw new CsgotmException(response);
            //}
            return response;
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
                    var classId = int.Parse(itemOnCsgotm.i_classid.ToString());
                    var instanceId = int.Parse(itemOnCsgotm.i_instanceid.ToString());
                    itemsOnCsgotm.Add(new ItemOnCsgotm(itemId, classId, instanceId, itemStatus,
                        botId));
                }
                //TODO return what I need in trades.
                //для изменения цены выставленного на продажу предмета нужно знать ui_id 
                //для передачи вещей - хз, мб. ui_id
                //для вывода вещей мне нужно забирать отсюда только список botid.
            }
            return itemsOnCsgotm;
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
        /// <param name="item">The item! :)</param>
        public static void StopSellingItem(ItemOnCsgotm item)
        {
            RenewPriceOrDeleteSellingItem(item, true);
        }

        public static void RenewPriceOnSellingItem(ItemOnCsgotm item)
        {
            RenewPriceOrDeleteSellingItem(item, false);
        }

        private static void RenewPriceOrDeleteSellingItem(ItemOnCsgotm item, bool stopSelling)
        {
            int price = 0;
            string itemName = "";
            if (!stopSelling)
            {
                SQLHelper sqlHelper = SQLHelper.getInstance();
                string class_instance = item.getClassInstance();
                ItemInSQL itemSql = sqlHelper.Select(class_instance);
                if (itemSql != null)
                {
                    itemName = itemSql.ItemName;
                    price = EvaluatePrice(itemSql);
                }
                else
                {
                    price = 0;
                }
            }
            else
            {
                price = 0;
            }
            string changePriceLink = String.Format("https://csgo.tm/api/SetPrice/{0}/{1}/?key={2}",item.ItemId,price,ApiKey);
            var changePriceResponse = CsgotmConfig.SteamWeb.Fetch(changePriceLink, "GET", null, false, null);
            if (changePriceResponse.Contains("\"result\":1"))
            {
                    Console.WriteLine("Price for item " + itemName + " has changed to " +
                                      (double) price/100 + ".руб");
            }
            else
            {
                Console.WriteLine("Error while changing item price: " + changePriceResponse);
            }
        }
        public static void SellItem(ItemInSQL item)
        {
            if (item.CanSell == 1)
            {
                int price = EvaluatePrice(item);
                var sellItemLink = string.Format("https://csgo.tm/api/SetPrice/new_{0}/{1}/?key={2}",
                    item.ClassInstance, price, ApiKey);
                var sellItemResponse = CsgotmConfig.SteamWeb.Fetch(sellItemLink, "GET", null, false, null);
                if (sellItemResponse.Contains("\"result\":1"))
                {
                    Console.WriteLine(item.ItemName + " is selling for price " + (double) price/100 + " .руб!");
                }
                else
                {
                    Console.WriteLine("Error while setting item to sell on website: " + sellItemResponse);
                }
            }
        }
        #region Timer Functions
        public static void StartPingPong(int delay)
        {
            //Perform it once in order to not wait for delay.
            Console.WriteLine("PingPong started.");
            System.Timers.Timer pingPongTimer = new System.Timers.Timer(delay);
            pingPongTimer.Elapsed += PingPong;
            pingPongTimer.AutoReset = true;
            pingPongTimer.Enabled = true;
        }
        private static void PingPong(Object source, ElapsedEventArgs e)
        {
            if (Monitor.TryEnter(PingPongLocker))
            {
                try
                {
                    if (!string.IsNullOrEmpty(ApiKey))
                    {
                        string pingpongLink = string.Format("https://csgo.tm/api/PingPong/?key={0}", ApiKey);
                        string response = CsgotmConfig.SteamWeb.Fetch(pingpongLink, "GET", null, false, null);
                        if (response.Contains("true"))
                        {
                            Console.WriteLine("PingPong true");
                            return;
                        }
                        else if (response.Contains("too early for pong"))
                        {
                            Console.WriteLine("PingPong true");
                            return;
                        }
                        else
                        {
                            Console.WriteLine("PingPong - FALSE");
                            Console.WriteLine(response);
                            return;
                        }
                    }
                    Console.WriteLine("Set API Key!");
                }
                finally
                {
                    Monitor.Exit(PingPongLocker);
                }
            }
        }
        public static void StartRenewPricesToAutoBuy(int delay)
        {
            System.Timers.Timer renewPricesTimer = new System.Timers.Timer(delay);
            renewPricesTimer.Elapsed += RenewAutoBuyPrices;
            renewPricesTimer.AutoReset = true;
            renewPricesTimer.Enabled = true;
            Console.WriteLine("Renew prices started");
        }

        /// <summary>
        /// sometimes bot struggles to send item to web-site. Writes something about correspond tradeoffers.
        /// but after updating inventory everything goes good.
        /// </summary>
        public static void UpdateInventory()
        {
            string updateLink = string.Format("https://csgo.tm/api/UpdateInventory/?key={0}", ApiKey);
            string response = CsgotmConfig.SteamWeb.Fetch(updateLink, "GET", null, false, null);
            if (response.Contains("true"))
            {
                Console.WriteLine("Inventory's updated!");
            }
            else
            {
                Console.WriteLine("Inventory's not updated. Response: ");
                Console.WriteLine(response);
            }
        }

        public static void StopSellingAllItems()
        {
            string stopLink = string.Format("https://csgo.tm/api/RemoveAll/?key={0}", ApiKey);
            string response = CsgotmConfig.SteamWeb.Fetch(stopLink, "GET", null, false, null);
            if (response.Contains("true"))
            {
                Console.WriteLine("All items removed from selling list");
            }
            else if (response.Contains("no_items_for_deletion"))
            {
                Console.WriteLine("No items in selling list");
            }
            else
            {
                Console.WriteLine("There was an error stopping selling: ");
                Console.WriteLine(response);
            }
        }

        public static void RemoveAllOrders()
        {
            string deleteLink = string.Format("https://csgo.tm/api/DeleteOrders/?key={0}", ApiKey);
            string response = CsgotmConfig.SteamWeb.Fetch(deleteLink, "GET", null, false, null);
            if (response.Contains("true"))
            {
                Console.WriteLine("Orders were succesfully removed!");
            }
            else if (response.Contains("There is no orders for delete."))
            {
                Console.WriteLine("No orders to delete.");
            }
            else
            {
                Console.WriteLine("There was an error deleting orders: ");
                Console.WriteLine(response);
            }
        }
        private static void RenewAutoBuyPrices(Object source, ElapsedEventArgs e)
        {
            if (Monitor.TryEnter(RenewAutobuyPricesLocker))
            {
                try
                {
                    Console.WriteLine("Renewing autobuy prices...");
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
                    Monitor.Exit(RenewAutobuyPricesLocker);
                }
            }
        }
        #endregion

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
        /// <summary>
        /// We don't check if we could sell this item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static int EvaluatePrice(ItemInSQL item)
        {
            int minPrice = item.MinPriceSell;
            int maxPrice = item.MaxPriceSell;
            int price = 0;
            string itemInfo = GetItemInfo(item.ClassInstance, "en", CsgotmConfig.SteamWeb);
            if (!itemInfo.Contains("error"))
            {
                dynamic itemInfoJson = JsonConvert.DeserializeObject(itemInfo);
                //TODO offers[0] could not exist - if there's no offers for this item.
                var currentLowestPrice = 0;
                for (int i = 0; i < 5; i++)
                {
                    var debug1 = Int32.Parse(itemInfoJson.offers[i].count.Value);
                    var debug2 = Int32.Parse(itemInfoJson.offers[i].my_count.Value);
                    if (debug1 > debug2)
                    {
                    //    if (itemInfoJson.offers[i].count > itemInfoJson.offers[i].my_count)
                    //{
                        currentLowestPrice = itemInfoJson.offers[i].price;
                        break;
                    }
                }
                var numberOfMySellingItems = itemInfoJson.offers[0].my_count;
                Console.WriteLine("Current price: " + (double)currentLowestPrice / 100 + " .руб");
                    if (numberOfMySellingItems == 0)
                    {
                        if (currentLowestPrice > maxPrice)
                        {
                            price = maxPrice;
                        }
                        else if (currentLowestPrice <= minPrice)
                        {
                            price = minPrice - 1;
                        }
                        else if (currentLowestPrice > minPrice)
                        {
                            price = currentLowestPrice - 1;
                        }
                    }
                    else
                    {
                        price = currentLowestPrice;
                    }
            }
            return price;
        }
    }

    class ItemOnCsgotm
    {
        public int ItemId { get; private set; }
        public int ClassId { get; private set; }
        public int InstanceId { get; private set; }
        public ItemOnCsgotmStatus Status { get; private set; }
        public int BotId { get; private set; }

        public ItemOnCsgotm(int item_id, int classId, int instanceId, int status, int botId)
        {
            this.ItemId = item_id;
            this.ClassId = classId;
            this.InstanceId = instanceId;
            //this.status = status;
            switch (status)
            {
                case 1:
                    this.Status = ItemOnCsgotmStatus.Selling;
                break;
                case 2:
                    this.Status = ItemOnCsgotmStatus.SendToCsgotmbot;
                    break;
                case 3:
                    this.Status = ItemOnCsgotmStatus.Bought;
                    break;
                case 4:
                    this.Status = ItemOnCsgotmStatus.ToReceiveFromCsgotmbot;
                    break;
            }
            this.BotId = botId;
        }

        public string getClassInstance()
        {
            return ClassId + "_" + InstanceId;
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
