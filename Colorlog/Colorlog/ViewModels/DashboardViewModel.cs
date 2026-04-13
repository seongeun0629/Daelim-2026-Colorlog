using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Colorlog.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        public string UserDisplayName { get; } = "Yeon";
        public string LastDiagnosisAtText { get; } = "오늘 09:12";

        public string PersonalColorName { get; } = "봄 웜 라이트";
        public ObservableCollection<ColorChip> BestColors { get; }

        public ObservableCollection<ProductRecommendation> RecentRecommendations { get; }

        public ObservableCollection<SkinMetric> SkinMetrics { get; }

        public ObservableCollection<FaceZoneTone> FaceZoneTones { get; }

        public DashboardViewModel()
        {
            BestColors = new ObservableCollection<ColorChip>
            {
                new("Peach Coral", "#FFEEA39A"),
                new("Warm Ivory", "#FFF2DFC8"),
                new("Soft Apricot", "#FFF4C9A8"),
                new("Rose Beige", "#FFDAB7A0"),
            };

            RecentRecommendations = new ObservableCollection<ProductRecommendation>
            {
                new("데일리 수분 톤업 선크림", "건조 + 톤 다운 개선", "4.7"),
                new("저자극 코랄 무드 블러셔", "홍조 커버 + 웜톤 정리", "4.6"),
                new("광채 세럼 쿠션 21N", "밝기 보정 + 밀착력", "4.5")
            };

            SkinMetrics = new ObservableCollection<SkinMetric>
            {
                new("홍조 지수", 42, 100, "보통", "#FFFB923C"),
                new("밝기 지수", 71, 100, "좋음", "#FF22C55E")
            };

            FaceZoneTones = new ObservableCollection<FaceZoneTone>
            {
                new("이마", "#FFDAB9A5"),
                new("양 볼", "#FFD8AE9A"),
                new("코 주변", "#FFE6C5AF"),
                new("턱", "#FFD4B49F")
            };
        }
    }

    public sealed class ColorChip
    {
        public string Name { get; }
        public Brush ToneBrush { get; }

        public ColorChip(string name, string hexColor)
        {
            Name = name;
            ToneBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor));
        }
    }

    public sealed class ProductRecommendation
    {
        public string ProductName { get; }
        public string Summary { get; }
        public string Rating { get; }

        public ProductRecommendation(string productName, string summary, string rating)
        {
            ProductName = productName;
            Summary = summary;
            Rating = rating;
        }
    }

    public sealed class SkinMetric
    {
        public string Name { get; }
        public int Current { get; }
        public int Max { get; }
        public string StatusLabel { get; }
        public Brush AccentBrush { get; }

        public double Percent => Max == 0 ? 0 : (double)Current / Max * 100d;

        public SkinMetric(string name, int current, int max, string statusLabel, string accentHexColor)
        {
            Name = name;
            Current = current;
            Max = max;
            StatusLabel = statusLabel;
            AccentBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentHexColor));
        }
    }

    public sealed class FaceZoneTone
    {
        public string ZoneName { get; }
        public Brush ZoneBrush { get; }

        public FaceZoneTone(string zoneName, string hexColor)
        {
            ZoneName = zoneName;
            ZoneBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor));
        }
    }
}
