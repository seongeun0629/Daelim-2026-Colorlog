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
        private readonly int _userId;

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

                    // BestColors — personal_color_types에서 가져오기
                    // (나중에 교체!!!!!, 지금은 진단 타입 기반 색상 표시)
                    BestColors.Clear();
                    BestColors.Add(new ColorChip(PersonalColorName, "#FFDAB9A5"));
                }
                else
                {
                    PersonalColorName = "진단 미실시";
                    LastDiagnosisAtText = "진단 기록 없음";

                    SkinMetrics.Clear();
                    SkinMetrics.Add(new SkinMetric("홍조 지수", 0, 100, "-", "#FFFB923C"));
                    SkinMetrics.Add(new SkinMetric("밝기 지수", 0, 100, "-", "#FF22C55E"));
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
