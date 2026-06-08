using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colorlog.Models
{
    public class RecommendedProduct
    {
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ProductUrl { get; set; } = string.Empty;
        public string RecReason { get; set; } = string.Empty;
        public string Rating { get; set; } = "-";
    }
}
