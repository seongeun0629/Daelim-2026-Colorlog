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
        public string PersonalColorName { get; set; } = string.Empty;
    }
}
