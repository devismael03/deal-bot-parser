using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using turbo_console.Models;

namespace turbo_console.Scrapers
{
    public class BinaAzScraper : IProductScraper
    {
        public string ScrapeFreshDeal(HtmlDocument doc, DateTime scrapeStarted)
        {
            //try catch whole function
            TimeSpan currentTimeSpan = new TimeSpan(scrapeStarted.Hour, scrapeStarted.Minute, 0);

            DateTime fiveMinutesAgo = scrapeStarted.Subtract(TimeSpan.FromMinutes(1));

            TimeSpan fiveMinutesAgoTimeSpan = new TimeSpan(fiveMinutesAgo.Hour, fiveMinutesAgo.Minute, 0);


            var productList = doc.DocumentNode.SelectSingleNode("//div[normalize-space(text())='ELANLAR']/..")?.NextSibling;

            var message = new StringBuilder();
            var counter = 1;

            if(productList == null)
            {
                return String.Empty;
            }

            foreach (var product in productList.ChildNodes)
            {

                var datePublished = product.SelectSingleNode(".//div[@class='city_when']")?.InnerText.Split(' ');
                

                if(datePublished == null)
                {
                    continue;
                }
                var name = product.SelectSingleNode(".//div[contains(concat(' ', @class, ' '),'location')]")?.InnerText ?? "";

                var priceVal = product.SelectSingleNode(".//span[contains(concat(' ', @class, ' '),'price-val')]")?.InnerText ?? "";
                var priceCur = product.SelectSingleNode(".//span[contains(concat(' ', @class, ' '),'price-cur')]")?.InnerText ?? "";
                var price = priceVal + " " + priceCur;

                var attributeElements= product.SelectSingleNode(".//ul[contains(concat(' ', @class, ' '),'name')]").ChildNodes.Select(li => li.InnerText);
                var attrs = string.Join(", ",attributeElements);
                var urlOfProperty = "https://bina.az" + product.SelectSingleNode(".//a[@class='item_link']").GetAttributeValue("href", "/");
                var datePublishedTimeSpan = TimeSpan.Parse(datePublished[2]);

                Product foundProduct = new Product(name, attrs, price, urlOfProperty);

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
                        Console.WriteLine($"Recent - {name} - {price} -  - {datePublished[2]}");
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
            var binaAzParse = Int32.TryParse(doc.DocumentNode.SelectSingleNode("//div[@class='price_header']//span[@class='price-val']").InnerText.Replace(" ", String.Empty), out int val);
            if (binaAzParse && val != link.CurrentPrice)
            {
                if(val > link.CurrentPrice)
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
