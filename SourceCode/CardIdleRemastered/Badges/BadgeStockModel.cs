using System;

namespace CardIdleRemastered.Badges
{
    public class BadgeStockModel
    {
        public string Name { get; set; }

        public int Count { get; set; }

        public double BadgePrice { get; set; }

        public double CardValue
        {
            get { return Math.Round(BadgePrice / Count, 2); }
        }
    }
}
