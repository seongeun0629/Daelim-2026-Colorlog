using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json.Linq;
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

            BestColors = new ObservableCollection<ColorChip>();
            RecentRecommendations = new ObservableCollection<ProductRecommendation>();

            UpdateBestColors();
            UpdateRecommendations();

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
                    UpdateBestColors();
                    UpdateRecommendations();
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

        private enum PersonalColorType { SpringWarm, SummerCool, AutumnWarm, WinterCool }

        private PersonalColorType ParseColorType(string name) =>
            name.Contains("봄") || name.Contains("Spring") ? PersonalColorType.SpringWarm :
            name.Contains("여름") || name.Contains("Summer") ? PersonalColorType.SummerCool :
            name.Contains("가을") || name.Contains("Autumn") ? PersonalColorType.AutumnWarm :
            name.Contains("겨울") || name.Contains("Winter") ? PersonalColorType.WinterCool :
            PersonalColorType.SpringWarm;

        private void UpdateBestColors()
        {
            var colorType = ParseColorType(_settings.PersonalColorName);
            ColorChip[] chips = colorType switch
            {
                PersonalColorType.SpringWarm => new[]
                {
                    new ColorChip("코랄", "#FFF5897F"),
                    new ColorChip("피치", "#FFFDBCB4"),
                    new ColorChip("아이보리", "#FFFFF5E4"),
                    new ColorChip("살구", "#FFFAC898"),
                },
                PersonalColorType.SummerCool => new[]
                {
                    new ColorChip("라벤더", "#FFBBA3D0"),
                    new ColorChip("블루", "#FFA5C1DC"),
                    new ColorChip("로즈", "#FFE8A0A0"),
                    new ColorChip("파우더핑크", "#FFF0C0C8"),
                },
                PersonalColorType.AutumnWarm => new[]
                {
                    new ColorChip("베이지", "#FFC4A882"),
                    new ColorChip("브라운", "#FF8B6348"),
                    new ColorChip("카키", "#FF7D8A5A"),
                    new ColorChip("머스타드", "#FFC9A227"),
                },
                PersonalColorType.WinterCool => new[]
                {
                    new ColorChip("블랙", "#FF2C2C2C"),
                    new ColorChip("화이트", "#FFF4F4F6"),
                    new ColorChip("와인", "#FF7B2040"),
                    new ColorChip("레드", "#FFC01840"),
                },
                _ => Array.Empty<ColorChip>()
            };

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                BestColors.Clear();
                foreach (var chip in chips)
                    BestColors.Add(chip);
            });
        }

        private void UpdateRecommendations()
        {
            var colorType = ParseColorType(_settings.PersonalColorName);
            ProductRecommendation[] items = colorType switch
            {
                PersonalColorType.SpringWarm => new[]
                {
                    new ProductRecommendation("코랄 무드 블러셔", "웜톤 혈색 보정 + 코랄 매칭", "4.7"),
                    new ProductRecommendation("살구빛 크림 립틴트", "봄웜 어울림 + 건조함 적음", "4.6"),
                    new ProductRecommendation("광채 세럼 쿠션 21N", "밝기 보정 + 웜베이지 매칭", "4.5"),
                },
                PersonalColorType.SummerCool => new[]
                {
                    new ProductRecommendation("라벤더 쉬머 블러셔", "쿨톤 혈색 + 청량감 부여", "4.7"),
                    new ProductRecommendation("로즈 틴트", "쿨핑크 + 자연스러운 착색", "4.6"),
                    new ProductRecommendation("나이아신아마이드 세럼", "미백 관리 + 쿨톤 피부 정리", "4.5"),
                },
                PersonalColorType.AutumnWarm => new[]
                {
                    new ProductRecommendation("브라운 무드 블러셔", "가을웜 깊이감 + 테라코타", "4.7"),
                    new ProductRecommendation("뮤트 버건디 립", "가을웜 어울림 + 차분한 포인트", "4.6"),
                    new ProductRecommendation("머스타드 아이 팔레트", "카키/머스타드 음영 매칭", "4.5"),
                },
                PersonalColorType.WinterCool => new[]
                {
                    new ProductRecommendation("쿨 로즈 블러셔", "겨울쿨 또렷한 혈색 부여", "4.7"),
                    new ProductRecommendation("선명한 레드 립", "겨울쿨 강렬한 포인트", "4.6"),
                    new ProductRecommendation("블루 언더톤 파운데이션", "쿨베이지 매칭 + 밀착력", "4.5"),
                },
                _ => Array.Empty<ProductRecommendation>()
            };

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                RecentRecommendations.Clear();
                foreach (var item in items)
                    RecentRecommendations.Add(item);
            });
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

        public void UpdateFromDiagnosis(JObject json)
        {
            var recs = json["recommendations"] as JArray;
            var typeInfo = json["color_type_info"];

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // 퍼스널컬러 이름 강제 갱신 (같은 값이어도 UI 반영)
                if (typeInfo != null)
                {
                    var typeName = typeInfo["type_name"]?.ToString();
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        // OnPropertyChanged를 직접 발생시켜 동일 값도 화면에 반영
                        _settings.PersonalColorName = "";
                        _settings.PersonalColorName = typeName;
                    }
                }

                // AI 추천 제품으로 RecentRecommendations 갱신
                if (recs != null && recs.Count > 0)
                {
                    RecentRecommendations.Clear();
                    foreach (var r in recs.Take(3))
                    {
                        RecentRecommendations.Add(new ProductRecommendation(
                            r["product_name"]?.ToString() ?? "",
                            r["reason"]?.ToString() ?? r["category"]?.ToString() ?? "",
                            "AI"
                        ));
                    }
                }
            });
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
