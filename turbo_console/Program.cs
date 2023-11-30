// See https://aka.ms/new-console-template for more information
using HtmlAgilityPack;
using Npgsql;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using turbo_console.Models;
using turbo_console.Scrapers;


namespace turbo_console
{

    class Program {
        /*
        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

        disable quickedit mode for console in production server
        */

        public static async Task Main(string[] args) {

            /*
            IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
            SetConsoleMode(handle, ENABLE_EXTENDED_FLAGS);
            */
            Stopwatch stopwatch = new Stopwatch();


            var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

            var connectionString = "db-connection-string";
            await using var dataSource = NpgsqlDataSource.Create(connectionString);





            while (await timer.WaitForNextTickAsync())
            {

                DateTime current = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Azerbaijan Standard Time"));
                var linksFromDb = new List<Link>();


                await using (var cmd = dataSource.CreateCommand("SELECT links.id, links.url, links.title, links.link_type, links.provider, links.current_price, links.last_price_change,users.telegram_id " +
                                                                "FROM links " +
                                                                "INNER JOIN \"AspNetUsers\" as users ON links.user_id=users.id"))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var link = new Link
                        {
                            Id = (System.Guid)reader["id"],
                            Title = reader["title"] as string,
                            Url = reader["url"] as string,
                            LinkType = (LinkTypeEnum?)(reader["link_type"] as int?),
                            Provider = (ProviderEnum?)(reader["provider"] as int?),
                            CurrentPrice = reader["current_price"] as int?,
                            LastPriceChange = reader["last_price_change"] as DateTime?,
                            TelegramId = reader["telegram_id"] as string
                        };
                        linksFromDb.Add(link);
                    }
                }



                Console.WriteLine(current);
                // Begin timing.
                stopwatch.Start();
                var linksGrouped = linksFromDb.GroupBy(link => link.TelegramId).Select(grp => grp.ToList()).ToList();
                Parallel.ForEach(linksGrouped, async (linkGroup) =>
                {
                    using (WebClient client = new WebClient())
                    {
                        var messageFreshDeal = new StringBuilder();
                        var messagePriceChecking = new StringBuilder();

                        var freshExists = false;
                        var priceCheckExists = false;
                        foreach (var link in linkGroup)
                        { 
                            try
                            {
                                IProductScraper scraper = link.Provider switch
                                {
                                    ProviderEnum.TurboAz => new TurboAzScraper(),
                                    ProviderEnum.BinaAz => new BinaAzScraper(),
                                    ProviderEnum.TapAz => new TapAzScraper()
                                };

                                string htmlCode = client.DownloadString(link.Url);
                                HtmlDocument doc = new HtmlDocument();

                                doc.LoadHtml(htmlCode);

                                if(link.LinkType == LinkTypeEnum.FreshDeal)
                                {
                                    var linkItems = scraper.ScrapeFreshDeal(doc, current);
                                    if (!String.IsNullOrEmpty(linkItems))
                                    {
                                        freshExists = true;
                                        messageFreshDeal.Append($"_{link.Title}_ \n {linkItems}").Append("\n\n");
                                    }
                                        
                                }
                                else
                                {
                                    var priceChangeMessage = scraper.ScrapePriceChange(doc,link);
                                    if (!String.IsNullOrEmpty(priceChangeMessage))
                                    {
                                        priceCheckExists = true;
                                        messagePriceChecking.Append($"_{link.Title}_ \n {priceChangeMessage} [Elana bax]({link.Url})").Append("\n\n");
                                        await using (var cmd = dataSource.CreateCommand("UPDATE links SET current_price=$1,last_price_change=$2 WHERE id=$3"))
                                        {
                                            cmd.Parameters.AddWithValue(link.CurrentPrice!);
                                            cmd.Parameters.AddWithValue(DateTime.UtcNow);
                                            cmd.Parameters.AddWithValue(link.Id);
                                            await cmd.ExecuteNonQueryAsync();
                                        }
                                    }
                                }
                                
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("EXCEPTION OCCURED !!!!" + e.Message);
                            }
                        }

                        try
                        {
                            using (HttpClient apiClient = new HttpClient())
                            {
                                var messageBodyBuilder = new StringBuilder();
                                if (freshExists)
                                {
                                    messageBodyBuilder.AppendLine($"*Fresh Deal Parsing Module* \n{messageFreshDeal.ToString()}\n");
                                }

                                if (priceCheckExists)
                                {
                                    messageBodyBuilder.AppendLine($"*Price Tracking Module* \n{messagePriceChecking.ToString()}");
                                }
                                
                                if(freshExists || priceCheckExists)
                                {
                                    var body = new
                                    {
                                        chat_id = linkGroup.First().TelegramId,
                                        text = messageBodyBuilder.ToString(),
                                        parse_mode = "Markdown"
                                    };
                                    apiClient.PostAsJsonAsync($"https://api.telegram.org/botTOKEN_HERE/sendMessage", body).GetAwaiter().GetResult();
                                }
                            }

                        }
                        catch (Exception)
                        {
                            Console.WriteLine("ERROR OCCURED DURING TELEGRAM MESSAGE SENDING");
                        }
                    }
                });
                stopwatch.Stop();
                Console.WriteLine("Elapsed: " + stopwatch.Elapsed);
                stopwatch.Reset();
            }

        }
    
    }



}


