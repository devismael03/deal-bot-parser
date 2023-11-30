using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace turbo_console.Models
{
    public class Product
    {
        public Product(string title, string attributes, string price, string url)
        {
            Title = title;
            Attributes = attributes;
            Price = price;
            Url = url;
        }

        public string Title { get; set; }
        public string Attributes { get; set; }
        public string Price { get; set; }
        public string Url { get; set; }
    }
}
