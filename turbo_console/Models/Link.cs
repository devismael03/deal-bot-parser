using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace turbo_console.Models
{
    public class Link
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public string? Title { get; set; }
        public LinkTypeEnum? LinkType { get; set; }
        public ProviderEnum? Provider { get; set; }
        public int? CurrentPrice { get; set; }
        public DateTime? LastPriceChange { get; set; }
        public string TelegramId { get; set; }

    }
}
