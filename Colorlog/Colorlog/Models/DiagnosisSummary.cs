using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colorlog.Models
{
    public class DiagnosisSummary
    {
        public string DiagnosisAt { get; set; } = string.Empty;
        public int Brightness { get; set; }
        public int Redness { get; set; }
        public int TypeId { get; set; }
        public string PersonalColorName { get; set; } = string.Empty;
        public string? OilyStatus { get; set; }
        public double? OilyScore { get; set; }

        public (int R, int G, int B)? ZoneForehead { get; set; }
        public (int R, int G, int B)? ZoneLCheek { get; set; }
        public (int R, int G, int B)? ZoneRCheek { get; set; }
        public (int R, int G, int B)? ZoneNose { get; set; }
        public (int R, int G, int B)? ZoneChin { get; set; }
    }
}
