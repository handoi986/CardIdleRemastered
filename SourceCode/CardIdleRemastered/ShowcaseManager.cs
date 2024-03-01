using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CardIdleRemastered.Badges;

namespace CardIdleRemastered
{
    public class ShowcaseManager
    {
        private readonly PricesUpdater _pricesUpdater;
        private Dictionary<string, BadgeShowcase> _cache;
        private ObservableCollection<BadgeShowcase> _showcases;

        public ShowcaseManager(PricesUpdater pricesUpdater)
        {
            _pricesUpdater = pricesUpdater;
        }

        public async Task<ObservableCollection<BadgeShowcase>> GetShowcases()
        {
            if (_showcases == null)
            {
                await ReadLocalStorage();
                _showcases = new ObservableCollection<BadgeShowcase>(_cache.Values.OrderBy(x => x.Title));
            }
            return _showcases;
        }

        public FileStorage Storage { get; set; }

        public async Task LoadShowcases(IEnumerable<BadgeModel> badges)
        {
            int unknownBadges = 0;

            await ReadLocalStorage();

            try
            {
                foreach (var badge in badges)
                {
                    BadgeShowcase showcase;
                    _cache.TryGetValue(badge.AppId, out showcase);

                    if (showcase != null)
                    {
                        UpdateCompletion(showcase, badge);
                        continue;
                    }

                    try
                    {
                        showcase = await SteamParser.GetBadgeShowcase(badge.AppId);
                        UpdateCompletion(showcase, badge);

                        _cache[badge.AppId] = showcase;
                        _showcases.Add(showcase);

                        unknownBadges++;
                    }
                    catch(Exception ex)
                    {
                        Logger.Exception(ex, String.Format("Showcase loading failed for {0}", badge.AppId));
                    }

                    await Task.Delay(1500);
                }
            }
            finally
            {
                if (unknownBadges > 0)
                    SaveLocalStorage();
            }
        }

        private static void UpdateCompletion(BadgeShowcase showcase, BadgeModel badge)
        {
            showcase.Title = badge.Title;
            showcase.UnlockedBadge = badge.UnlockedBadge;
            showcase.IsCompleted = badge.UnlockedBadge != null;
            showcase.CanCraft = badge.CanCraft;
            showcase.IsOwned = true;
            foreach (var level in showcase.CommonBadges)
                level.IsCompleted = level.Name == badge.UnlockedBadge;
        }

        private async Task ReadLocalStorage()
        {
            if (_cache != null)
                return;

            _cache = new Dictionary<string, BadgeShowcase>();

            string db = Storage.ReadContent();
            if (String.IsNullOrWhiteSpace(db))
                return;

            var apps = await SteamParser.GetSteamApps();

            try
            {
                var xml = XDocument.Parse(db).Root;
                if (xml != null)
                {
                    foreach (var xeBadge in xml.Elements("badge"))
                    {
                        var appId = (string)xeBadge.Attribute("app_id");
                        string title = appId;

                        GameIdentity app;
                        bool marketable = apps.TryGetValue(appId, out app);
                        if (marketable)
                        {
                            title = app.name;
                        }

                        var showcase = new BadgeShowcase(appId, title);
                        showcase.IsMarketable = marketable;

                        BadgeStockModel stock;
                        if (_pricesUpdater.Prices.TryGetValue(appId, out stock))
                        {
                            showcase.CardPrice = stock.CardValue;
                            showcase.BadgePrice = stock.BadgePrice;
                            if (!marketable)
                                showcase.Title = stock.Name;
                        }

                        foreach (var xeLevel in xeBadge.Elements("level"))
                            showcase.CommonBadges.Add(BadgeFromXml(xeLevel));

                        var xeFoil = xeBadge.Element("foil");
                        if (xeFoil != null)
                        {
                            var foilBadge = BadgeFromXml(xeFoil);
                            if (false == String.IsNullOrEmpty(foilBadge.PictureUrl))
                                showcase.FoilBadge = foilBadge;
                        }

                        _cache.Add(appId, showcase);
                    }
                }

                Logger.Info("Showcase storage initialized");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Showcase storage");
            }
        }

        private void SaveLocalStorage()
        {
            var badges = _cache.Values.Select(app =>
                new XElement("badge",
                    new XAttribute("app_id", app.AppId),
                    app.CommonBadges.Select(b => BadgeToXml("level", b)),
                    BadgeToXml("foil", app.FoilBadge)));

            var xml = new XElement("badges", badges);

            Storage.WriteContent(xml.ToString());
        }

        private XElement BadgeToXml(string nodeName, BadgeLevelData badge)
        {
            var xe = new XElement(nodeName);

            if (badge != null)
            {
                xe.Add(new XAttribute("url", badge.PictureUrl),
                    new XAttribute("lvl", badge.Level),
                    new XAttribute("name", badge.Name));
            }

            return xe;
        }

        private BadgeLevelData BadgeFromXml(XElement xe)
        {
            return new BadgeLevelData
                   {
                       Level = (string)xe.Attribute("lvl"),
                       Name = (string)xe.Attribute("name"),
                       PictureUrl = (string)xe.Attribute("url")
                   };
        }
    }
}
