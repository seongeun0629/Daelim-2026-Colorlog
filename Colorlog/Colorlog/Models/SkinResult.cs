using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colorlog.Models
{
    public class SkinResult
    {
        public long timestamp { get; set; }
        public bool face_detected { get; set; }
        public LightingData lighting { get; set; }
        public SkinToneData skin_tone { get; set; }
    }

    public class LightingData
    {
        public double brightness { get; set; }
    }

    public class SkinToneData
    {
        public int r { get; set; }
        public int g { get; set; }
        public int b { get; set; }
        public string hex { get; set; }
    }
}
