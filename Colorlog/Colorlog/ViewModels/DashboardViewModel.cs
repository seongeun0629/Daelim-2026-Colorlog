using Colorlog.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Media;

namespace Colorlog.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private int _userId;


        [ObservableProperty]
        private string _userDisplayName = "사용자";

        [ObservableProperty]
        public string _lastDiagnosisAtText = "오늘 00:00";

        [ObservableProperty]
        public string _personalColorName = "봄 웜 라이트";
        public ObservableCollection<ColorChip> BestColors { get; } = new();
        public ObservableCollection<ProductRecommendation> RecentRecommendations { get; }
        public ObservableCollection<SkinMetric> SkinMetrics { get; } = new();
        public ObservableCollection<FaceZoneTone> FaceZoneTones { get; }
        public DashboardViewModel(DatabaseService databaseService, int userId)
        {
            _databaseService = databaseService;
            _userId = userId;

            //더비 데이터 유지 (나중에 교체)!!!
            //BestColors = new ObservableCollection<ColorChip>
            //{
            //    new("Peach Coral", "#FFEEA39A"),
            //    new("Warm Ivory", "#FFF2DFC8"),
            //    new("Soft Apricot", "#FFF4C9A8"),
            //    new("Rose Beige", "#FFDAB7A0"),
            //};

            RecentRecommendations = new ObservableCollection<ProductRecommendation>();

            //SkinMetrics = new ObservableCollection<SkinMetric>
            //{
            //    new("홍조 지수", 42, 100, "보통", "#FFFB923C"),
            //    new("밝기 지수", 71, 100, "좋음", "#FF22C55E")
            //};

            FaceZoneTones = new ObservableCollection<FaceZoneTone>();

            LoadFromDatabase();
        }

        private void LoadFromDatabase()
        {
            try
            {
                // 1. 유저 이름
                var user = _databaseService.GetUserById(_userId);
                if (user != null)
                    UserDisplayName = user.UserName;

                // 2. 최근 진단 결과
                var diagnosis = _databaseService.GetLatestDiagnosis(_userId);
                if (diagnosis != null)
                {
                    // 퍼스널 컬러
                    PersonalColorName = string.IsNullOrEmpty(diagnosis.PersonalColorName)
                        ? "진단 미실시" : diagnosis.PersonalColorName;

                    // 마지막 진단 시각
                    if (DateTime.TryParse(diagnosis.DiagnosisAt, out var dt))
                        LastDiagnosisAtText = dt.ToString("yyyy.MM.dd HH:mm",
                            CultureInfo.CurrentCulture);

                    // 피부 지표
                    SkinMetrics.Clear();
                    SkinMetrics.Add(new SkinMetric("홍조 지수", diagnosis.Redness, 100,
                        GetMetricLabel(diagnosis.Redness), "#FFFB923C"));
                    SkinMetrics.Add(new SkinMetric("밝기 지수", diagnosis.Brightness, 100,
                        GetMetricLabel(diagnosis.Brightness), "#FF22C55E"));

                    if (diagnosis.OilyScore.HasValue)
                    {
                        var oilyInt = (int)diagnosis.OilyScore.Value;
                        var oilyLabel = diagnosis.OilyStatus switch
                        {
                            "Oily" => "유분 많음",
                            "Normal" => "정상",
                            "Not Oily" => "건조",
                            _ => "-"
                        };
                        SkinMetrics.Add(new SkinMetric("유분 지수", oilyInt, 100, oilyLabel, "#FF8B5CF6"));
                    }

                    // BestColors — personal_color_types에서 가져오기
                    BestColors.Clear();
                    if (diagnosis.TypeId > 0)
                    {
                        var colorNames = _databaseService.GetColorsByTypeId(diagnosis.TypeId);
                        var colorMap = new Dictionary<string, string>
                        {
                            { "피치",         "#FFFFA07A" },
                            { "살구색",       "#FFFFD4A8" },
                            { "아이보리",     "#FFFFF0E0" },
                            { "연한 코랄",    "#FFFF9988" },
                            { "코랄",         "#FFFF6B6B" },
                            { "오렌지",       "#FFFFA500" },
                            { "선명한 노랑",  "#FFFFE066" },
                            { "밝은 그린",    "#FF90EE90" },
                            { "라벤더",       "#FFE6CCFF" },
                            { "파우더핑크",   "#FFFFB6C1" },
                            { "블루",         "#FFADD8E6" },
                            { "민트",         "#FF98FFD2" },
                            { "로즈",         "#FFFF9999" },
                            { "모브",         "#FFCC99BB" },
                            { "회색빛 블루",  "#FF99AABB" },
                            { "연보라",       "#FFCC99FF" },
                            { "브라운",       "#FFA0785A" },
                            { "버건디",       "#FF8B1A2A" },
                            { "카키",         "#FF8B8B6B" },
                            { "올리브",       "#FF808000" },
                            { "베이지",       "#FFF5F0E8" },
                            { "머스타드",     "#FFFFE066" },
                            { "테라코타",     "#FFCC6644" },
                            { "카멜",         "#FFC19A6B" },
                            { "블랙",         "#FF2D2D2D" },
                            { "화이트",       "#FFF8F8F8" },
                            { "선명한 레드",  "#FFEE1122" },
                            { "로얄블루",     "#FF4169E1" },
                            { "네이비",       "#FF001F5B" },
                            { "다크 버건디",  "#FF5C0A14" },
                            { "차콜",         "#FF444444" },
                            { "다크 플럼",    "#FF4A0040" },
                            { "누드",         "#FFE8CEB0" },
                            { "그레이지",     "#FFBDB5A6" },
                            { "내추럴 베이지","#FFF0DEC0" },
                            { "소프트 화이트","#FFF5F5F0" },
                        };

                        foreach (var name in colorNames)
                        {
                            var hex = colorMap.TryGetValue(name, out var h) ? h : "#FFDAB9A5";
                            BestColors.Add(new ColorChip(name, hex));
                        }
                    }
                    else
                    {
                        BestColors.Add(new ColorChip(PersonalColorName, "#FFDAB9A5"));
                    }

                    FaceZoneTones.Clear();
                    void AddZone(string name, (int R, int G, int B)? zone)
                    {
                        if (zone.HasValue)
                            FaceZoneTones.Add(new FaceZoneTone(name,
                                $"#FF{zone.Value.R:X2}{zone.Value.G:X2}{zone.Value.B:X2}"));
                    }
                    AddZone("이마", diagnosis.ZoneForehead);
                    AddZone("왼쪽 볼", diagnosis.ZoneLCheek);
                    AddZone("오른쪽 볼", diagnosis.ZoneRCheek);
                    AddZone("코 주변", diagnosis.ZoneNose);
                    AddZone("턱", diagnosis.ZoneChin);

                    if (FaceZoneTones.Count == 0)
                        FaceZoneTones.Add(new FaceZoneTone("진단 후 표시됩니다", "#FFDAB9A5"));
                }
                else
                {
                    PersonalColorName = "진단 미실시";
                    LastDiagnosisAtText = "진단 기록 없음";

                    SkinMetrics.Clear();
                    SkinMetrics.Add(new SkinMetric("홍조 지수", 0, 100, "-", "#FFFB923C"));
                    SkinMetrics.Add(new SkinMetric("밝기 지수", 0, 100, "-", "#FF22C55E"));
                    SkinMetrics.Add(new SkinMetric("유분 지수", 0, 100, "-", "#FF8B5CF6"));
                }

                var recs = _databaseService.GetLatestRecommendations(_userId);
                RecentRecommendations.Clear();
                foreach (var rec in recs.Take(3))
                {
                    RecentRecommendations.Add(new ProductRecommendation(
                        rec.ProductName,
                        rec.RecReason,
                        rec.Rating
                    ));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Dashboard] LoadFromDatabase 오류: {ex.Message}");
            }
        }

        private static string GetMetricLabel(int value) => value switch
        {
            >= 70 => "좋음",
            >= 40 => "보통",
            _ => "나쁨"
        };

        public void UpdateUserId(int userId)
        {
            Debug.WriteLine($"[Dashboard] UpdateUserId 호출: {userId}");

            _userId = userId;
            LoadFromDatabase();
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
