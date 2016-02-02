using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json;
using SteamBot;
using SteamBot.Csgotm;
using SteamTrade;

namespace Csgotm
{
    class ItemInSQL
    {
        public int _n { get; }
        public int Context { get; }
        public string ClassInstance { get; }
        public string ItemName { get; }
        public string ItemUrl { get; }
        public string ItemUrlSteam { get; }
        public string Hash { get; }
        public int CanBuy { get; }
        public int CanSell { get; }
        public int MaxToBuy { get; }
        public int Bought { get; }
        public int MinPriceToBuy { get; }
        public int MaxPriceToBuy { get; }

        private const string UrlStart1 = "https://market.csgo.com/item/";
        private const string UrlStart2 = "https://csgo.tm/item/";
        private const int CsgoContext = 3;

        public ItemInSQL(int n,int context, String classInstance, string itemName, string itemUrl, string itemUrlSteam, string hash,
            int canBuy,int canSell, int maxToBuy, int bought, int minPriceToBuy, int maxPriceToBuy)
        {
            this._n = n;
            this.Context = context;
            this.ClassInstance = classInstance;
            this.ItemName = itemName;
            this.ItemUrl = itemUrl;
            this.ItemUrlSteam = itemUrlSteam;
            this.Hash = hash;
            this.CanBuy = canBuy;
            this.CanSell = canSell;
            this.MaxToBuy = maxToBuy;
            this.Bought = bought;
            this.MinPriceToBuy = minPriceToBuy;
            this.MaxPriceToBuy = maxPriceToBuy;
        }
        public ItemInSQL(int context, String classInstance, string itemName, string itemUrl, string itemUrlSteam, string hash,
           int canBuy,int canSell, int maxToBuy, int bought, int minPriceToBuy, int maxPriceToBuy)
        {
            this.Context = context;
            this.ClassInstance = classInstance;
            this.ItemName = itemName;
            this.ItemUrl = itemUrl;
            this.ItemUrlSteam = itemUrlSteam;
            this.CanBuy = canBuy;
            this.CanSell = canSell;
            this.MaxToBuy = maxToBuy;
            this.Bought = bought;
            this.MinPriceToBuy = minPriceToBuy;
            this.MaxPriceToBuy = maxPriceToBuy;
        }
        //TODO: rewrite without passing apiKey to constructor. Constructor should get API_Key somehow by itself.
        public ItemInSQL(string itemUrl, string apiKey, SteamWeb steamWeb, int canBuy = 1,int canSell = 1, int maxToBuy = 100, int minPriceToBuy = 0, int maxPriceToBuy = 0)
        {
            this.Context = ParseContext(itemUrl);
            this.ClassInstance = ParseClassInstance(itemUrl);
            this.ItemUrl = itemUrl;
            this.ItemUrlSteam = "";

            //getting hash and name for item.
            string itemInfo = CsgotmAPI.GetItemInfo(ClassInstance,"en",apiKey, steamWeb);
            dynamic jsonItemInfo = JsonConvert.DeserializeObject(itemInfo);
            this.Hash = jsonItemInfo.hash.ToString();
            this.ItemName = jsonItemInfo.market_name.ToString();




            this.CanBuy = canBuy;
            this.CanSell = canSell;
            this.MaxToBuy = maxToBuy;
            this.Bought = 0;
            this.MinPriceToBuy = minPriceToBuy;
            this.MaxPriceToBuy = maxPriceToBuy;
        }

        public string GetItemClass()
        {
            string itemClass = ClassInstance.Substring(0, ClassInstance.IndexOf("_"));
            return itemClass;
        }
        public string GetItemInstance()
        {
            string itemInstance = ClassInstance.Substring(ClassInstance.IndexOf("_") + 1);
            return itemInstance;
        }

        private string ParseClassInstance(string url)
        {
            //https://market.csgo.com/item/520025252-0-Operation+Breakout+Weapon+Case/
            if (url.Contains(UrlStart1))
            {
                url = url.Substring(UrlStart1.Length);
            }
            else if (url.Contains(UrlStart2))
            {
                url = url.Substring(UrlStart2.Length);
            }
            else
            {
                throw new CsgotmException("not item url!");
            }
            string itemClass = url.Substring(0, url.IndexOf("-"));//getting class
            url = url.Substring(itemClass.Length + 1); //deleting class
            string itemInstance = url.Substring(0, url.IndexOf("-"));
            return itemClass + "_" + itemInstance;
        }

        private int ParseContext(string url)
        {
            if (url.Contains(UrlStart1))
            {
                return CsgoContext;
            }
            if (url.Contains(UrlStart2))
            {
                return CsgoContext;
            }
            throw new CsgotmException("Can't parse context from URL - probably URL is wrong ");
        }

    }
    
}
