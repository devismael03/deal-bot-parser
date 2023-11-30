using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using turbo_console.Models;

namespace turbo_console.Scrapers
{
    public interface IProductScraper
    {
        public string ScrapeFreshDeal(HtmlDocument doc, DateTime scrapeStarted);
        public string ScrapePriceChange(HtmlDocument doc,Link link);

    }
}
