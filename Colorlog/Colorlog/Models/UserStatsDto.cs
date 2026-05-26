using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Colorlog.Models
{
    public class UserStatsDto
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("user_name")]
        public string UserName { get; set; } = string.Empty;

        [JsonPropertyName("diagnosis_count")]
        public int DiagnosisCount { get; set; }

        [JsonPropertyName("join_date")]
        public string JoinDate { get; set; } = string.Empty;

        [JsonPropertyName("latest_color_type")]
        public string LatestColorType { get; set; } = string.Empty;

        [JsonPropertyName("latest_at")]
        public string LatestAt { get; set; } = string.Empty;

        [JsonPropertyName("latest_brightness")]
        public int LatestBrightness { get; set; }

        [JsonPropertyName("latest_redness")]
        public int LatestRedness { get; set; }
    }

}
