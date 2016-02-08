using SteamKit2;
using SteamTrade;
using SteamTrade.TradeOffer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Timers;
using System.Web;
using Csgotm;
using CsgotmBot;
using Newtonsoft.Json;
using SteamKit2.GC.Dota.Internal;
using SteamAuth;
using SteamBot.Csgotm;
using SteamKit2.GC.TF2.Internal;
using SteamKit2.Internal;
using SteamTrade.TradeWebAPI;
using TradeAsset = SteamTrade.TradeOffer.TradeOffer.TradeStatusUser.TradeAsset;

namespace SteamBot
{
    public class CsgotmUserHandler : UserHandler
    {
        public CsgotmUserHandler(Bot bot, SteamID sid) : base(bot, sid) { }
        static object outItemsLocker = new object();
        static object putOnSaleLocker = new object();
        public override void OnNewTradeOffer(TradeOffer offer)
        {
            //receiving a trade offer 
            if (!IsAdmin)
            {
                //                so every item has classid, instanceid and assetid
                //                classid - id of the same type of item, so I.E.all "Glock | Fade"(Factory New) weapons will have same same classid, smth like 12345
                //                instanceid - is 0 for items, who have no differences between them.I.e.cases, stickers, boxes - all will have instanceid 0.
                //                and all weapons should have instanceid, because weapon of same type could be different - i.e. "Glock | Fade"(Factory New) could have different patterns.
                //                and assetid(or just id) - is an unique number of the exact item.So 1 assetid = 1 item.
                //                BEWARE, assetid is changed every time traded


                //parse inventories of bot and other partner
                //either with webapi or generic inventory
                //Bot.GetInventory();
                //Bot.GetOtherInventory(OtherSID);

                //offer.Items.AddMyItem(730, 2, 926978479, 1);//this code should add the item "Chroma 2 case to offer". Context always 2, seems. No instance?O_o

                //var myItems = offer.Items.GetMyItems();
                //var theirItems = offer.Items.GetTheirItems();
                //foreach (var item in theirItems)
                //{
                //    var contextLong = item.ContextId;
                //    var contextString = contextLong.ToString();
                //    Log.Success(contextString);
                //}
                //Log.Info("They want " + myItems.Count + " of my items.");
                //Log.Info("And I will get " +  theirItems.Count + " of their items.");

                ////do validation logic etc
                //if (DummyValidation(myItems, theirItems))
                //{
                //    string tradeid;
                //    if (offer.Accept(out tradeid))
                //    {
                //Bot.AcceptAllMobileTradeConfirmations();
                //Log.Success("HAHAHAHAH");
                        //Log.Success("Accepted trade offer successfully : Trade ID: " + tradeid);
                //        }
                //    }
                //    else
                //    {
                //        // maybe we want different items or something

                //        //offer.Items.AddMyItem(0, 0, 0);
                //        //offer.Items.RemoveTheirItem(0, 0, 0);
                //        if (offer.Items.NewVersion)
                //        {
                //            string newOfferId;
                //            if (offer.CounterOffer(out newOfferId))
                //            {
                //                Bot.AcceptAllMobileTradeConfirmations();
                //                Log.Success("Counter offered successfully : New Offer ID: " + newOfferId);
                //            }
                //        }
                //    }
                //}
                //else
                //{
                //    //we don't know this user so we can decline
                //    if (offer.Decline())
                //    {
                //        Log.Info("Declined trade offer : " + offer.TradeOfferId + " from untrusted user lol " + OtherSID.ConvertToUInt64());
                //    }
            }
        }
        public override void OnMessage(string message, EChatEntryType type)
        {
            if (IsAdmin)
            {
                //creating a new trade offer
                var offer = Bot.NewTradeOffer(OtherSID);

                //offer.Items.AddMyItem(0, 0, 0);
                if (offer.Items.NewVersion)
                {
                    string newOfferId;
                    if (offer.Send(out newOfferId))
                    {
                        Bot.AcceptAllMobileTradeConfirmations();
                        Log.Success("Trade offer sent : Offer ID " + newOfferId);
                    }
                }

                //creating a new trade offer with token
                var offerWithToken = Bot.NewTradeOffer(OtherSID);

                //offer.Items.AddMyItem(0, 0, 0);
                if (offerWithToken.Items.NewVersion)
                {
                    string newOfferId;
                    // "token" should be replaced with the actual token from the other user
                    if (offerWithToken.SendWithToken(out newOfferId, "token"))
                    {
                        Bot.AcceptAllMobileTradeConfirmations();
                        Log.Success("Trade offer sent : Offer ID " + newOfferId);
                    }
                }
            }
        }

        public override void OnBotCommand(string command)
        {
            switch (command)
            {
                //case "RenewPrices":
                //    ItemInSQL item = new ItemInSQL(2, "520025252_0", "Operation Breakout Weapon Case",
                //        "https://market.csgo.com/item/520025252-0-Operation+Breakout+Weapon+Case/", "",1,10,0,1,1);
                //    //TODO verify API_KEY somehow
                //    CsgotmAPI.SetAutoBuy(item, API_KEY);
                //break;
            }
                
        }
        public override bool OnGroupAdd() { return false; }

        public override bool OnFriendAdd() { return IsAdmin; }

        public override void OnFriendRemove() { }

        public override void OnLoginCompleted() {
            //StartItemsReceiving(30000);
            StartInAndOutItems(40000);
            StartPutOnSellingItems(30000);
            CsgotmAPI.StartPingPong(181000);
            CsgotmAPI.StartRenewPricesToAutoBuy(60000);

            //Bot.GetInventory();lol, this command returns inventory for teamfortress 2 items only.

            




            //Log.Info("Items to sell on csgo.tm " + itemsInInventoryToSell.Count);

            //TODO test what's faster or this
            //List<ItemInSQL> list = sqlHelper.SelectAll();
            //foreach (var item in myInventory.items)
            //{
            //    if (list.Find(i => i.ClassInstance.Equals(item.Value.descriptionid)) != null)
            //    {
            //        Console.WriteLine("--------------------------------------------------");
            //    }
            //    else
            //    {
            //        Console.WriteLine("--------------------------------------------------");
            //    }
            //}

            //string cookieFilePath = GetCookiesFilePath();
            //CookieContainer cookieContainer = ReadCookiesFromDisk(cookieFilePath);
            //SteamTrade.SteamWeb steamWeb = Bot.SteamWeb;
            //steamWeb._cookies = cookieContainer;

            //int tryLogin = 0;
            //int maxtryLogin = 5;
            //int tryBuyItem = 0;
            //int maxtryBuyItem = 3;

            //Boolean canCreateAPI = false;


            ////here I use dynamic cuz json doesn't have any fields or methods before initializing. But after I get deserialize response,
            ////it'll have fields. If I use Object - compiler will check that Object doesn't have field(in example) 'First' and will not allow me
            ////to compile the programm. And if i'll try to get non-existing field from dynamic, (in example) 'THATSNOTREALFIELDHAHA', throws exception
            //dynamic json = JsonConvert.DeserializeObject(steamWeb.Fetch("https://csgo.tm/api/Trades/?key=" + API_KEY, "GET", null,
            //    false, null));
            //foreach (var item in json)
            //{
            //    Log.Success(item.ui_status.ToString());
            //}
            //while (!canCreateAPI)
            //{

            //    String responseCreatingApi = steamWeb.Fetch("https://csgo.tm/botinfo/", "GET", null, false, null);
            //    //here we must check if we could create a new API key for csgo.tm
            //    if (responseCreatingApi.Contains("Чтобы создать ключ, выполните вход."))
            //    {
            //        tryLogin++;
            //        if (tryLogin <= maxtryLogin)
            //        {
            //            Log.Success("Trying to login to csgo.tm..." + tryLogin);
            //            LoginToCsgotm(steamWeb);
            //            continue;
            //        }
            //        Log.Error("After " + maxtryLogin + " tries login to csgo.tm was unsuccessfull. Probably steamCookies expired.");
            //        //TODO handle unsuccessfull login to csgotm
            //        break;
            //    }
            //    if (responseCreatingApi.Contains("Для создания API ключа вам необходимо"))
            //    {
            //        String response =
            //            steamWeb.Fetch(
            //                "https://csgo.tm/api/ItemRequest/out/271260132/?key=", "GET",
            //                null, false, null);
            //        WriteCookiesToDisk(cookieFilePath, steamWeb._cookies);
            //        //tryBuyItem++;
            //        //if (tryBuyItem <= maxtryBuyItem)
            //        //{
            //        //    Log.Success("Trying to buy one item..." + tryBuyItem);
            //        //    //TODO покупка за 1 руб. Make restrictions for numbers of try. Max 1 try to buy item. Put here NORMAL way to buy it
            //        //    //Для покупки использовать временный api-key(переменная firstAPIKEY) которую должен указать юзер. Да и ссылку временную. Все временное.
            //        //    string responseFromBuyingItem = steamWeb.Fetch(TEMP_LINK_FOR_BUYING_ONE_ITEM, "GET", null,
            //        //        false, null);
            //        //    if (responseFromBuyingItem.Contains("ok"))
            //        //    {
            //        //        Log.Success("Seems we bought the item. Change the way I did it to normal when buying/accepting items will be ready.");
            //        //        //And here I must 
            //        //    }
            //        //    continue;
            //        //}
            //        //Log.Error("After " + maxtryBuyItem + " tries Buying 1 item was unsuccessfull.");
            //        ////TODO handle unsuccessfull itemBuying
            //        //break;
            //    }
            //    canCreateAPI = true;
            //    //TODO создаем API KEY
            //}


            ////TODO: check response for correct login


            ////response = steamWeb.Fetch("https://csgo.tm/check/", "GET", null, false, null); //and here we retreive TOlink for checking trades. 
            ////String tradeofferLink = GetTradeLinkForCheckingTradeOffersOnCsgotm(response);
            ////Uri myUri = new Uri(tradeofferLink);
            ////string partner = HttpUtility.ParseQueryString(myUri.Query).Get("partner");
            ////string token = HttpUtility.ParseQueryString(myUri.Query).Get("token");
            ////uint accountID = uint.Parse(partner);
            ////SteamID partnerSteamId = new SteamID(accountID, EUniverse.Public, EAccountType.Individual);
            ////response = steamWeb.Fetch("https://steamcommunity.com/trade/" + partnerSteamId +
            ////                          "/foreigninventory/sessionid=" + steamWeb.SessionId + "&steamid=" + partnerSteamId +
            ////                          "&appid=730&contextid=2", "GET", null, false, null);
            ////response = steamWeb.Fetch("http://steamcommunity.com/profiles/76561197983865523/inventory/json/730/2",
            ////    "GET", null, false, null);
            ////var smth = JsonConvert.DeserializeObject(response);
            ////ForeignInventory foreignInventory = new ForeignInventory(smth);


            ////GenericInventory theirInventory = new GenericInventory(steamWeb);
            ////List<long> contextId = new List<long>();
            ////contextId.Add(2);
            ////theirInventory.load(730,contextId,partnerSteamId);
            ////TradeOffer tradeOffer = Bot.NewTradeOffer(partnerSteamId);
            ////foreach (var item in theirInventory.items)
            ////{
            ////    tradeOffer.Items.AddTheirItem(730, 2, (long)item.Key, 1);
            ////}
            ////string tradeofferId;
            ////bool success = tradeOffer.SendWithToken(out tradeofferId, token);
            ////Log.Success("treadeoffer sent " + success);

            ////Here I must click on the fucking button "Я выполнил условия обмена", but it sends GET for address that idk.
            ////fucking seems that there's smth on the page happens.(In Browser) Because when I send tradeoffer with closed page 
            ////and tradeoffer successfully sent, csgo.tm still says I didn't send TO. So fucking 3rd way to check TO availability.


            ////SteamID "STEAM_0:1:122894859", 76561198206055447. AccountID - 245789719
            //Log.Success("Logged in to csgo.tm successfully!");
            //WriteCookiesToDisk(cookieFilePath, steamWeb._cookies);
            //////TODO:try to receive API key. And if success - return true. If no - try to login again.
        }

        public override bool OnTradeRequest() { return false; }

        public override void OnTradeError(string error) { }

        public override void OnTradeTimeout() { }

        public override void OnTradeSuccess() { }

        public override void OnTradeAwaitingConfirmation(long tradeOfferID) { }

        public override void OnTradeInit() { }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeMessage(string message) { }

        public override void OnTradeReady(bool ready) { }

        public override void OnTradeAccept() { }
        //New
        public static String GetAttributeFromResponse(String responseData, String findingAttribute)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            TextReader tr = new StringReader(responseData);
            doc.Load(tr);
            string selector = "//input[@name = '" + findingAttribute + "']";
            var node = doc.DocumentNode.SelectSingleNode(selector);
            foreach (var attribute in node.Attributes)
            {
                if (attribute.Name == "value")
                {
                    return attribute.Value;
                }
            }
            return "";
        }
        private static String GetTradeLinkForCheckingTradeOffersOnCsgotm(String responseData)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            TextReader tr = new StringReader(responseData);
            doc.Load(tr);
            string selector = "//textarea";
            var node = doc.DocumentNode.SelectSingleNode(selector);
            return node.InnerText;
        }

        private static void getAllCookies(CookieContainer cookies)
        {
            BugFix_CookieDomain(cookies);
            //Hashtable table = (Hashtable)cookies.GetType().InvokeMember("m_domainTable",
            //                                                             BindingFlags.NonPublic |
            //                                                             BindingFlags.GetField |
            //                                                             BindingFlags.Instance,
            //                                                             null,
            //                                                             cookies,
            //                                                             new object[] { });



            //foreach (var key in table.Keys)
            //{
            //    if (key.ToString().IndexOf(".") == 0)
            //    {
            //    }
            //    foreach (Cookie cookie in cookies.GetCookies(new Uri(string.Format("http://{0}/", key))))
            //        {
            //            Console.WriteLine("Name = {0} ; Value = {1} ; Domain = {2}", cookie.Name, cookie.Value,
            //                cookie.Domain);
            //        }
            //}

        }
        private static void BugFix_CookieDomain(CookieContainer cookies)
        {
            System.Type _ContainerType = typeof(CookieContainer);
            Hashtable table = (Hashtable)_ContainerType.InvokeMember("m_domainTable",
                                       System.Reflection.BindingFlags.NonPublic |
                                       System.Reflection.BindingFlags.GetField |
                                       System.Reflection.BindingFlags.Instance,
                                       null,
                                       cookies,
                                       new object[] { });
            //ArrayList keys = new ArrayList(table.Keys);
            //foreach (string keyObj in keys)
            //{
            //    string key = (keyObj as string);
            //    if (key[0] == '.')
            //    {
            //        string newKey = key.Remove(0, 1);
            //        table[newKey] = table[keyObj];
            //    }
            //}
            foreach (var key in table.Keys)
            {
                string keyString = key.ToString();
                if (keyString[0] == '.')
                {
                    keyString = keyString.Remove(0, 1);
                }
                //Сделано неправильно, как я понял, т.к. если попадается домен вида .dota2.net, то мы просто забираем
                //Куки для .dota2.net, при этом куки которые записаны на dota2.net я вообще не трогаю и не вижу. Нужно это проверить.
                foreach (Cookie cookie in cookies.GetCookies(new Uri(string.Format("http://{0}/", keyString))))
                   {
                        Console.WriteLine("Name = {0} ; Value = {1} ; Domain = {2}", cookie.Name, cookie.Value,
                            cookie.Domain);
                   }
            }
        }
        public static void WriteCookiesToDisk(string file, CookieContainer cookieJar)
        {
            using (Stream stream = File.Create(file))
            {
                try
                {
                    Console.Out.Write("Writing cookies to disk... ");
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, cookieJar);
                    Console.Out.WriteLine("Done.");
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine("Problem writing cookies to disk: " + e.GetType());
                }
            }
        }

        public Boolean LoginToCsgotm(SteamTrade.SteamWeb steamWeb)
        {
            try
            {
                String response = steamWeb.Fetch("https://csgo.tm/login/", "GET", null, false, "https://csgo.tm/");

                /*csgo.tm redirects us to Openid-Steam-login page, where we must imitate pression of "sign in" button. For it, on
            Openid-Steam-login page Steam gives us special data - nonce, which is a one-time-used key for login. And openidparams -
            seems as public RSA-key, but not sure. We need to retreive it from response and post it to the link below.
            */

                NameValueCollection postData = new NameValueCollection();
                String nonce = GetAttributeFromResponse(response, "nonce");
                String openidparams = GetAttributeFromResponse(response, "openidparams");
                postData.Add("action", "steam_openid_login");
                postData.Add("openid.mode", "checkid_setup");
                postData.Add("openidparams", openidparams);
                postData.Add("nonce", nonce);
                steamWeb.Fetch("https://steamcommunity.com/openid/login", "POST", postData, false, null);
                Log.Success("Logged in successfully");
                return true;
                //here we're loggining
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return false;
            }
        }
        public static CookieContainer ReadCookiesFromDisk(string file)
        {

            try
            {
                using (Stream stream = File.Open(file, FileMode.Open))
                {
                    Console.Out.Write("Reading cookies from disk... ");
                    BinaryFormatter formatter = new BinaryFormatter();
                    Console.Out.WriteLine("Done.");
                    return (CookieContainer)formatter.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Problem reading cookies from disk: " + e.GetType());
                return new CookieContainer();
            }
        }

        private static string GetCookiesFilePath()
        {
            string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cookiesFolder = Path.Combine(executableLocation, "cookies");
            Directory.CreateDirectory(cookiesFolder);
            return Path.Combine(cookiesFolder, "cookies.cookie");
        }

        private void StartItemsReceiving(int delay)
        {
            System.Timers.Timer aTimer = new System.Timers.Timer(delay);
            aTimer.Elapsed += OutItemsFromCsgotm;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
            Log.Success("Automatically receiving items started.");
        }
        private void OutItemsFromCsgotm(Object source, ElapsedEventArgs e)
        {
            if (Monitor.TryEnter(outItemsLocker))
            {
                try
                {
                    List<ItemOnCsgotm> itemsOnCsgotm = CsgotmAPI.GetTrades();
                    Log.Info("Checking if there're items to receive");
                    foreach (var itemOnCsgotm in itemsOnCsgotm)
                    {
                        if (itemOnCsgotm.Status == ItemOnCsgotmStatus.ToReceiveFromCsgotmbot)
                        {
                            //TODO handle errors if they appear.
                            //Info from bot while we send OUT request for item
                            //{"success":true,"trade":"983185053","nick":"Shepard","botid":239950058,"profile":"https:\/\/steamcommunity.com\/profiles\/76561198200215786\/","secret":"FI4X"}
                            Log.Info("Trying to out items...");
                            string tradeInfo = CsgotmAPI.ItemRequest(false, itemOnCsgotm.BotId.ToString());
                            if (tradeInfo.Contains("true"))
                            {
                                Log.Info("Trying to accept tradeoffer...");
                                TradeOffer tradeOffer;
                                dynamic jsonTradeInfo = JsonConvert.DeserializeObject(tradeInfo);
                                string trade = jsonTradeInfo.trade.ToString();
                                string secretPhrase = jsonTradeInfo.secret.ToString();
                                Bot.TryGetTradeOffer(trade, out tradeOffer);
                                if (tradeOffer != null && tradeOffer.Message == secretPhrase)
                                {
                                    if (tradeOffer.Accept())
                                    {
                                        Log.Success("Items are received!");
                                    }
                                }
                            }
                            else
                            {
                                Log.Error(tradeInfo);
                            }
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(outItemsLocker);
                }
            }
        }
#region Timers
        private void StartInAndOutItems(int delay)
        {
            System.Timers.Timer inAndOutTimer = new System.Timers.Timer(delay);
            inAndOutTimer.Elapsed += InAndOutItems;
            inAndOutTimer.AutoReset = true;
            inAndOutTimer.Enabled = true;
            Log.Success("Automatically receiving and sending items started.");
        }
        private void InAndOutItems(Object source, ElapsedEventArgs e)
        {
            if (Monitor.TryEnter(outItemsLocker))
            {
                try
                {
                    var itemsOnCsgotm = CsgotmAPI.GetTrades();
                    List<ItemOnCsgotm> itemsSelling =  ItemsByStatus(itemsOnCsgotm, ItemOnCsgotmStatus.Selling);
                    List<ItemOnCsgotm> itemsToSend = ItemsByStatus(itemsOnCsgotm, ItemOnCsgotmStatus.SendToCsgotmbot);
                    List<ItemOnCsgotm> itemsBought = ItemsByStatus(itemsOnCsgotm, ItemOnCsgotmStatus.Bought);
                    List<ItemOnCsgotm> itemsToReceive = ItemsByStatus(itemsOnCsgotm, ItemOnCsgotmStatus.ToReceiveFromCsgotmbot);
                        //foreach (var itemStatus in status)
                        //{
                        //    Console.WriteLine(itemStatus.botId);
                        //}
                    Console.WriteLine("--------------------------------------------------------------");
                    Console.WriteLine("Items selling: " + itemsSelling.Count);
                    Console.WriteLine("Items to send: " + itemsToSend.Count);
                    Console.WriteLine("Items waiting to receive: " + itemsBought.Count);
                    Console.WriteLine("Items ready to receive: " + itemsToReceive.Count);
                    Console.WriteLine("--------------------------------------------------------------");

                    //TODO send items
                    ItemsInToWebsite(itemsToSend);
                    //Log.Error("SENDING ITEMS HASN'T DONE CODED YET");
                    //receiving items
                    ItemsOutFromWebsite(itemsToReceive);
                }
                finally
                {
                    Monitor.Exit(outItemsLocker);
                }
            }
        }
        public void StartPutOnSellingItems(int delay)
        {
            System.Timers.Timer renewPricesTimer = new System.Timers.Timer(delay);
            renewPricesTimer.Elapsed += PutOnSellingItems;
            renewPricesTimer.AutoReset = true;
            renewPricesTimer.Enabled = true;
            Console.WriteLine("Put on selling items started.");
        }
        private void PutOnSellingItems(Object source, ElapsedEventArgs e)
        {
            if (Monitor.TryEnter(putOnSaleLocker))
            {
                try
                {
                    SQLHelper sqlHelper = SQLHelper.getInstance();
                    GenericInventory myInventory = new GenericInventory(SteamWeb);
                    List<long> contexts = new List<long>();
                    List<ItemOnCsgotm> itemsOnCsgotm = CsgotmAPI.GetTrades();
                    contexts.Add(2);
                    myInventory.load(730, contexts, Bot.SteamUser.SteamID);
                    //TODO test what's faster this
                    List<ItemOnCsgotm> itemsSelling = ItemsByStatus(itemsOnCsgotm, ItemOnCsgotmStatus.Selling);
                    List<ItemOnCsgotm> itemsToSend = ItemsByStatus(itemsOnCsgotm, ItemOnCsgotmStatus.SendToCsgotmbot);
                    List<ItemOnCsgotm> sellingAndSendingItems = itemsSelling.Concat(itemsToSend).ToList();
                    /*
            items in my inventory that we're able to sell on csgo.tm
                */
                    /*items in bot's inventory
            for which we have a record in sql. If in inventory we find class_instance, for which we don't have a record
            in sql - we skip this item.
            */
                    //TODO I think I can create on request to SQL to retreive all items from it.
                    foreach (var description in myInventory.descriptions)
                    {
                        if (description.Value.tradable)
                        {
                            ItemInSQL itemInSql = sqlHelper.Select(description.Key); 
                            if (itemInSql != null)
                            {
                                var itemsInInventory =
                                    myInventory.items.Where(item => item.Value.descriptionid == description.Key);
                                var csgotmItems =
                                    sellingAndSendingItems.Where(
                                        item => (item.ClassId + "_" + item.InstanceId) == description.Key);
                                if (!csgotmItems.Any())
                                {
                                    //probably we've got a weapon with instance_id !=0, so trying to find items only by class
                                    csgotmItems = sellingAndSendingItems.Where(
                                        item => (item.ClassId  == description.Value.classid));
                                }
                                if (itemsInInventory.Count() > csgotmItems.Count())
                                {
                                    int numberOfItemsToSell = itemsInInventory.Count() - csgotmItems.Count();
                                    for (int i = 0; i < numberOfItemsToSell; i++)
                                    {
                                        CsgotmAPI.SellItem(itemInSql);
                                    }
                                }
                                if (itemsInInventory.Count() < csgotmItems.Count())
                                {
                                    int numberOfItemsToDelete = csgotmItems.Count() - itemsInInventory.Count();
                                    var numerator = csgotmItems.GetEnumerator();
                                    for (int i = 0; i < numberOfItemsToDelete; i++)
                                    {
                                        numerator.MoveNext();
                                        CsgotmAPI.StopSellingItem(numerator.Current);
                                    }
                                }
                                //Update price for items
                                foreach (var item in csgotmItems)
                                {
                                    CsgotmAPI.RenewPriceOnSellingItem(item);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(putOnSaleLocker);
                }
            }
        }
#endregion

        /// <summary>
        /// Method returns list of items on web-site for given status.
        /// I.E. we
        /// </summary>
        /// <returns></returns>
        private List<ItemOnCsgotm> ItemsByStatus(List<ItemOnCsgotm> itemsOnCsgotm, ItemOnCsgotmStatus givenStatus)
        {
            var groupedByStatus = itemsOnCsgotm.GroupBy(o => o.Status);
            foreach (var status in groupedByStatus)
            {
                if (status.Key == givenStatus)
                {
                    List<ItemOnCsgotm> itemsSelling = status.ToList();
                    return itemsSelling;
                }
                if (status.Key == givenStatus)
                {
                    List<ItemOnCsgotm>  itemsToSend = status.ToList();
                    return itemsToSend;
                }
                if (status.Key == givenStatus)
                {
                    List<ItemOnCsgotm>  itemsBought = status.ToList();
                    return itemsBought;
                }
                if (status.Key == givenStatus)
                {
                    List<ItemOnCsgotm> itemsToReceive = status.ToList();
                    return itemsToReceive;
                }
                //foreach (var itemStatus in status)
                //{
                //    Console.WriteLine(itemStatus.botId);
                //}
            }
            //if no items for given status was found - just return empty list.
            return new List<ItemOnCsgotm>();
        }

        private void ItemsOutFromWebsite(List<ItemOnCsgotm> itemsToOut)
        {
            foreach (ItemOnCsgotm itemOnCsgotm in itemsToOut)
            {
                    //TODO handle errors if they appear.
                    //Info from bot while we send OUT request for item
                    //{"success":true,"trade":"983185053","nick":"Shepard","botid":239950058,"profile":"https:\/\/steamcommunity.com\/profiles\/76561198200215786\/","secret":"FI4X"}
                    Log.Info("Trying to out items...");
                    string tradeInfo = CsgotmAPI.ItemRequest(false, itemOnCsgotm.BotId.ToString());
                    if (tradeInfo.Contains("true"))
                    {
                        Log.Info("Trying to accept tradeoffer...");
                        TradeOffer tradeOffer;
                        dynamic jsonTradeInfo = JsonConvert.DeserializeObject(tradeInfo);
                        string trade = jsonTradeInfo.trade.ToString();
                        string secretPhrase = jsonTradeInfo.secret.ToString();
                        Bot.TryGetTradeOffer(trade, out tradeOffer);
                        if (tradeOffer != null && tradeOffer.Message == secretPhrase)
                        {
                            if (tradeOffer.Accept())
                            {
                                Log.Success("Items are received!");
                            }
                            else
                            {
                                Log.Error("Invalid tradeoffer!");
                            }
                        }
                    }
                    else
                    {
                        Log.Error(tradeInfo);
                    }
            }
        }

        private void ItemsInToWebsite(List<ItemOnCsgotm> itemsIn)
        {
            if (itemsIn.Count != 0)
            {
                Log.Info("Trying to send items...");
                string tradeInfo = CsgotmAPI.ItemRequest(true, "1");
                if (tradeInfo.Contains("true"))
                {
                    Log.Info("Trying to accept tradeoffer...");
                    TradeOffer tradeOffer;
                    dynamic jsonTradeInfo = JsonConvert.DeserializeObject(tradeInfo);
                    string trade = jsonTradeInfo.trade.ToString();
                    string secretPhrase = jsonTradeInfo.secret.ToString();
                    Bot.TryGetTradeOffer(trade, out tradeOffer);
                    if (tradeOffer != null && tradeOffer.Message == secretPhrase)
                    {
                        tradeOffer.Accept();
                        Thread.Sleep(3000);
                        Bot.AcceptAllMobileTradeConfirmations();
                    }
                    else
                    {
                        Log.Error("Invalid tradeoffer!");
                    }
                }
                else
                {
                    CsgotmAPI.UpdateInventory();
                    Thread.Sleep(10000);//give csgotm time to update inventory.
                    Log.Error(tradeInfo);
                }
            }
        }
    }
}
