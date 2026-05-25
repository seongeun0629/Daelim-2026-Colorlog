using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace Colorlog.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly SettingsViewModel _settings;

        // SettingsViewModel에서 실시간으로 읽어오는 속성들
        public string UserDisplayName => _settings.UserName;
        public string PersonalColorName => _settings.PersonalColorName;
        public string LastDiagnosisAtText => _settings.LatestDiagnosisAtText;

        public ObservableCollection<ColorChip> BestColors { get; }
        public ObservableCollection<ProductRecommendation> RecentRecommendations { get; }
        public ObservableCollection<SkinMetric> SkinMetrics { get; }
        public ObservableCollection<FaceZoneTone> FaceZoneTones { get; }

        public DashboardViewModel(SettingsViewModel settings)
        {
            _settings = settings;
            _settings.PropertyChanged += OnSettingsChanged;

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

            SkinMetrics = new ObservableCollection<SkinMetric>();
            UpdateSkinMetrics();

            FaceZoneTones = new ObservableCollection<FaceZoneTone>
            {
                new("이마", "#FFDAB9A5"),
                new("양 볼", "#FFD8AE9A"),
                new("코 주변", "#FFE6C5AF"),
                new("턱", "#FFD4B49F")
            };
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SettingsViewModel.UserName):
                    OnPropertyChanged(nameof(UserDisplayName));
                    break;
                case nameof(SettingsViewModel.PersonalColorName):
                    OnPropertyChanged(nameof(PersonalColorName));
                    break;
                case nameof(SettingsViewModel.LatestDiagnosisAtText):
                    OnPropertyChanged(nameof(LastDiagnosisAtText));
                    break;
                case nameof(SettingsViewModel.LatestBrightness):
                case nameof(SettingsViewModel.LatestRedness):
                    UpdateSkinMetrics();
                    break;
            }
        }

        private void UpdateSkinMetrics()
        {
            int redness = _settings.LatestRedness >= 0 ? _settings.LatestRedness : 0;
            int brightness = _settings.LatestBrightness >= 0 ? _settings.LatestBrightness : 0;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                SkinMetrics.Clear();
                SkinMetrics.Add(new SkinMetric("홍조 지수", redness, 100, GetRednessLabel(redness), GetRednessColor(redness)));
                SkinMetrics.Add(new SkinMetric("밝기 지수", brightness, 100, GetBrightnessLabel(brightness), GetBrightnessColor(brightness)));
            });
        }

        private static string GetRednessLabel(int v) => v <= 30 ? "낮음" : v <= 60 ? "보통" : "높음";
        private static string GetRednessColor(int v) => v <= 30 ? "#FF22C55E" : v <= 60 ? "#FFFB923C" : "#FFEF4444";
        private static string GetBrightnessLabel(int v) => v <= 40 ? "어두움" : v <= 70 ? "보통" : "좋음";
        private static string GetBrightnessColor(int v) => v <= 40 ? "#FF9CA3AF" : v <= 70 ? "#FFFB923C" : "#FF22C55E";
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
