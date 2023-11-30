using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using turbo_console.Models;

namespace turbo_console.Scrapers
{
    public class TurboAzScraper : IProductScraper
    {
        public string ScrapeFreshDeal(HtmlDocument doc, DateTime scrapeStarted)
        {

            TimeSpan currentTimeSpan = new TimeSpan(scrapeStarted.Hour, scrapeStarted.Minute, 0);

            DateTime fiveMinutesAgo = scrapeStarted.Subtract(TimeSpan.FromMinutes(1));

            TimeSpan fiveMinutesAgoTimeSpan = new TimeSpan(fiveMinutesAgo.Hour, fiveMinutesAgo.Minute, 0);

            var productList = doc.DocumentNode.SelectSingleNode("//p[normalize-space(text())='ELANLAR']/../..").NextSibling.LastChild;

            var message = new StringBuilder();
            var counter = 1;

            foreach (var product in productList.ChildNodes)
            {
                
                var datePublished = product.SelectSingleNode(".//div[@class='products-i__datetime']").InnerText.Split(' ');
                var name = product.SelectSingleNode(".//div[contains(concat(' ', @class, ' '),'products-i__name')]")?.InnerText;
                var price = product.SelectSingleNode(".//div[contains(concat(' ', @class, ' '),'product-price')]")?.InnerText;
                var attrs = product.SelectSingleNode(".//div[contains(concat(' ', @class, ' '),'products-i__attributes')]")?.InnerText;
                var urlOfAuto = "https://turbo.az" + product.SelectSingleNode(".//a[@class='products-i__link']").GetAttributeValue("href", "/");
                var datePublishedTimeSpan = TimeSpan.Parse(datePublished[2]);

                Product foundProduct = new Product(name, attrs, price, urlOfAuto);

                bool conditionToCheck = datePublishedTimeSpan >= fiveMinutesAgoTimeSpan && datePublished[1] == "bugün";
                TimeSpan startOfDay = new TimeSpan(0, 0, 0);
                List<int> exceptional = new List<int> { 0, 1, 2, 3, 4 }; //if not 5 minutes this should change
                if (scrapeStarted.Hour == 0 && exceptional.Contains(scrapeStarted.Minute))
                {
                    conditionToCheck = (datePublishedTimeSpan >= fiveMinutesAgoTimeSpan && datePublished[1] == "dünən") || (datePublishedTimeSpan >= startOfDay && datePublished[1] == "bugün");
                }

                if (conditionToCheck)
                {
                    if (datePublishedTimeSpan != currentTimeSpan)
                    {
                        message.AppendLine($"{counter++}. {foundProduct.Title}   {foundProduct.Attributes}   {foundProduct.Price} \n [Elana bax]({foundProduct.Url})");
                        Console.WriteLine($"Recent - {name} - {price} - {attrs} - {datePublished[2]}");
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
            return "";
        }
    }
}
