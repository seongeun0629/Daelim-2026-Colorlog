using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Colorlog.Models
{
    public partial class FaceTonePreviewItem : ObservableObject
    {
        [ObservableProperty]
        private string _zoneName;

        [ObservableProperty]
        private string _colorHex;

        [ObservableProperty]
        private Brush _toneBrush;

        [ObservableProperty]
        private int _confidencePercent;
    }
}
