using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using turbo_console.Models;

namespace turbo_console.Scrapers
{
    public class TapAzScraper : IProductScraper
    {
        public string ScrapeFreshDeal(HtmlDocument doc, DateTime scrapeStarted)
        {

            TimeSpan currentTimeSpan = new TimeSpan(scrapeStarted.Hour, scrapeStarted.Minute, 0);

            DateTime fiveMinutesAgo = scrapeStarted.Subtract(TimeSpan.FromMinutes(1));

            TimeSpan fiveMinutesAgoTimeSpan = new TimeSpan(fiveMinutesAgo.Hour, fiveMinutesAgo.Minute, 0);

            var productList = doc.DocumentNode.SelectSingleNode("//h2[normalize-space(text())='Elanlar']/../..").NextSibling;

            var message = new StringBuilder();
            var counter = 1;

            foreach (var product in productList.ChildNodes)
            {

                var datePublished = product.SelectSingleNode(".//div[@class='products-created']").InnerText.Split(' ');
                var name = product.SelectSingleNode(".//div[contains(concat(' ', @class, ' '),'products-name')]")?.InnerText ?? "";

                name = HttpUtility.HtmlDecode(name);

                var priceVal = product.SelectSingleNode(".//span[contains(concat(' ', @class, ' '),'price-val')]")?.InnerText ?? "";
                var priceCur = product.SelectSingleNode(".//span[contains(concat(' ', @class, ' '),'price-cur')]")?.InnerText ?? "";
                var price = priceVal + " " + priceCur;

                var urlOfProperty = "https://tap.az" + product.SelectSingleNode(".//a[@class='products-link']").GetAttributeValue("href", "/");
                var datePublishedTimeSpan = TimeSpan.Parse(datePublished[2]);

                Product foundProduct = new Product(name, "", price, urlOfProperty);

                bool conditionToCheck = datePublishedTimeSpan >= fiveMinutesAgoTimeSpan && datePublished[1] == "bugün,";
                TimeSpan startOfDay = new TimeSpan(0, 0, 0);
                List<int> exceptional = new List<int> { 0, 1, 2, 3, 4 }; //if not 5 minutes this should change
                if (scrapeStarted.Hour == 0 && exceptional.Contains(scrapeStarted.Minute))
                {
                    conditionToCheck = (datePublishedTimeSpan >= fiveMinutesAgoTimeSpan && datePublished[1] == "dünən,") || (datePublishedTimeSpan >= startOfDay && datePublished[1] == "bugün,");
                }

                if (conditionToCheck)
                {
                    if (datePublishedTimeSpan != currentTimeSpan)
                    {
                        message.AppendLine($"{counter++}. {foundProduct.Title}   {foundProduct.Attributes}   {foundProduct.Price} \n [Elana bax]({foundProduct.Url})");
                        Console.WriteLine($"Recent - {name} - {price} - {datePublished[2]}");
                    }
                }
                else
                {
                    break;
                }

            }

            return message.ToString();
        }

        public string ScrapePriceChange(HtmlDocument doc, Link link)
        {
            string message = String.Empty;
            var tapAzParse = Int32.TryParse(doc.DocumentNode.SelectSingleNode("//div[@class='lot-header fixed-product-info']//span[@class='price-val']").InnerText.Replace(" ", String.Empty), out int val);
            if (tapAzParse && val != link.CurrentPrice)
            {
                if (val > link.CurrentPrice)
                {
                    message = $"Elanın qiyməti artıb. Öncəki qiymət: {link.CurrentPrice}, Hal-hazırki qiymət: {val}";
                }
                else
                {
                    message = $"Elanın qiyməti düşüb. Öncəki qiymət: {link.CurrentPrice}, Hal-hazırki qiymət: {val}";
                }
                link.CurrentPrice = val;
                link.LastPriceChange = DateTime.Now;
            }


            return message;
        }
    }
}
