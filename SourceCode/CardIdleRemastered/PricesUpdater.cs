using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CardIdleRemastered.Badges;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CardIdleRemastered
{
    public class PricesUpdater
    {
        private Dictionary<string, BadgeStockModel> _prices;

        public FileStorage Storage { get; set; }

        public Dictionary<string, BadgeStockModel> Prices
        {
            get
            {
                if (_prices == null)
                {
                    var values = Storage.ReadContent();
                    _prices = JsonConvert.DeserializeObject<Dictionary<string, BadgeStockModel>>(values);
                }
                return _prices;
            }
        }

        public async Task<bool> DownloadCatalog()
        {
            try
            {
                string json = await SteamParser.DownloadString("https://www.steamcardexchange.net//api/request.php?GetBadgePrices_Guest");
                var template = new { data = new object[0] };
                var src = JsonConvert.DeserializeAnonymousType(json, template);
                var badges = new Dictionary<string, BadgeStockModel>();
                foreach(JArray item in src.data)
                {
                    var game = (JArray)item[0];
                    var badge = new BadgeStockModel
                    {
                        Name = game[1].ToString(),
                        BadgePrice = Convert.ToDouble(item[2].ToString().Trim('$'), CultureInfo.InvariantCulture),
                        Count = Convert.ToInt32(item[1].ToString(), CultureInfo.InvariantCulture),
                    };
                    badges[game[0].ToString()] = badge;
                }
                Storage.WriteContent(JsonConvert.SerializeObject(badges));
                _prices = null;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "");
                return false;
            }
            return true;
        }

        public BadgeStockModel GetStockModel(string id)
        {
            BadgeStockModel b = null;
            if (Prices != null)
            {
                Prices.TryGetValue(id, out b);
            }
            return b;
        }
    }
}
