using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Colorlog.ViewModels
{
    public partial class BeautyLogViewModel : ObservableObject
    {
        private readonly SettingsViewModel _settings;
        private bool _hasAiRecommendations = false;

        public string UserDisplayName => _settings.UserName;
        public string LastDiagnosisAtText => _settings.LatestDiagnosisAtText;

        [ObservableProperty]
        private string toneSummary = string.Empty;

        [ObservableProperty]
        private string skinConditionSummary = string.Empty;

        public ObservableCollection<string> FocusKeywords { get; } = new();
        public ObservableCollection<BeautyProductCard> MakeupItems { get; } = new();
        public ObservableCollection<BeautyProductCard> SkinCareItems { get; } = new();

        public BeautyLogViewModel(SettingsViewModel settings)
        {
            _settings = settings;
            _settings.PropertyChanged += OnSettingsChanged;
            RefreshAll();
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SettingsViewModel.UserName):
                    OnPropertyChanged(nameof(UserDisplayName));
                    break;
                case nameof(SettingsViewModel.LatestDiagnosisAtText):
                    OnPropertyChanged(nameof(LastDiagnosisAtText));
                    break;
                case nameof(SettingsViewModel.PersonalColorName):
                case nameof(SettingsViewModel.LatestBrightness):
                case nameof(SettingsViewModel.LatestRedness):
                    RefreshAll();
                    break;
            }
        }

        public void UpdateFromDiagnosis(JObject json)
        {
            var recs = json["recommendations"] as JArray;
            if (recs == null || recs.Count == 0)
            {
                System.IO.File.AppendAllText(@"C:\Users\alice\colorlog_debug.txt",
                    $"[{DateTime.Now:HH:mm:ss}] UpdateFromDiagnosis: recs null/empty\n");
                return;
            }

            var makeupCats = new HashSet<string> { "치크", "립", "아이", "베이스" };

            var makeupCards = recs
                .Where(r => makeupCats.Contains(r["category"]?.ToString() ?? ""))
                .Select(r => new BeautyProductCard(
                    r["product_name"]?.ToString() ?? "",
                    r["category"]?.ToString() ?? "",
                    r["reason"]?.ToString() ?? "",
                    "AI",
                    new[] { "AI 추천", "퍼스널컬러 기반", "올리브영" },
                    r["product_url"]?.ToString() ?? ""
                )).ToList();

            var skinCards = recs
                .Where(r => !makeupCats.Contains(r["category"]?.ToString() ?? ""))
                .Select(r => new BeautyProductCard(
                    r["product_name"]?.ToString() ?? "",
                    r["category"]?.ToString() ?? "",
                    r["reason"]?.ToString() ?? "",
                    "AI",
                    new[] { "AI 추천", "퍼스널컬러 기반", "올리브영" },
                    r["product_url"]?.ToString() ?? ""
                )).ToList();

            var catList = string.Join(",", recs.Select(r => r["category"]?.ToString()));
            System.IO.File.AppendAllText(@"C:\Users\alice\colorlog_debug.txt",
                $"[{DateTime.Now:HH:mm:ss}] categories={catList} makeup={makeupCards.Count} skin={skinCards.Count}\n");

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                System.IO.File.AppendAllText(@"C:\Users\alice\colorlog_debug.txt",
                    $"[{DateTime.Now:HH:mm:ss}] Dispatcher.Invoke running on UI thread\n");

                if (makeupCards.Count > 0)
                {
                    MakeupItems.Clear();
                    foreach (var c in makeupCards) MakeupItems.Add(c);
                    _hasAiRecommendations = true;
                }
                if (skinCards.Count > 0)
                {
                    SkinCareItems.Clear();
                    foreach (var c in skinCards) SkinCareItems.Add(c);
                    _hasAiRecommendations = true;
                }

                System.IO.File.AppendAllText(@"C:\Users\alice\colorlog_debug.txt",
                    $"[{DateTime.Now:HH:mm:ss}] After update: MakeupItems={MakeupItems.Count} SkinCareItems={SkinCareItems.Count} firstName={MakeupItems.FirstOrDefault()?.Name ?? "none"}\n");
            });
        }

        private void RefreshAll()
        {
            var colorType = ParseColorType(_settings.PersonalColorName);
            ToneSummary = BuildToneSummary(colorType);
            SkinConditionSummary = BuildSkinConditionSummary(colorType);
            UpdateCollections(colorType);
        }

        private enum PersonalColorType { SpringWarm, SummerCool, AutumnWarm, WinterCool }

        private static PersonalColorType ParseColorType(string name) =>
            name.Contains("봄") || name.Contains("Spring") ? PersonalColorType.SpringWarm :
            name.Contains("여름") || name.Contains("Summer") ? PersonalColorType.SummerCool :
            name.Contains("가을") || name.Contains("Autumn") ? PersonalColorType.AutumnWarm :
            name.Contains("겨울") || name.Contains("Winter") ? PersonalColorType.WinterCool :
            PersonalColorType.SpringWarm;

        private static string BuildToneSummary(PersonalColorType colorType) => colorType switch
        {
            PersonalColorType.SpringWarm => "봄 웜톤 · 코랄/피치 계열 고채도 포인트 추천",
            PersonalColorType.SummerCool => "여름 쿨톤 · 라벤더/로즈 계열 부드러운 포인트 추천",
            PersonalColorType.AutumnWarm => "가을 웜톤 · 브라운/머스타드 계열 차분한 포인트 추천",
            PersonalColorType.WinterCool => "겨울 쿨톤 · 와인/레드 계열 선명한 포인트 추천",
            _ => string.Empty
        };

        private string BuildSkinConditionSummary(PersonalColorType colorType)
        {
            var baseText = colorType switch
            {
                PersonalColorType.SpringWarm => "웜 베이스 + 밝은 발색으로 화사하고 생기있는 메이크업",
                PersonalColorType.SummerCool => "쿨 베이스 + 은은한 발색으로 청량하고 투명한 메이크업",
                PersonalColorType.AutumnWarm => "웜 딥 베이스 + 뮤트 톤으로 차분하고 성숙한 메이크업",
                PersonalColorType.WinterCool => "쿨 딥 베이스 + 선명한 발색으로 강렬하고 또렷한 메이크업",
                _ => string.Empty
            };

            var brightness = _settings.LatestBrightness;
            var redness = _settings.LatestRedness;
            if (brightness < 0 && redness < 0)
                return baseText;

            var bText = brightness >= 0 ? (brightness <= 40 ? "밝기 낮음" : brightness <= 70 ? "밝기 보통" : "밝기 양호") : "";
            var rText = redness >= 0 ? (redness <= 30 ? "홍조 없음" : redness <= 60 ? "홍조 약간" : "홍조 있음") : "";
            var conditions = string.Join(" · ", new[] { bText, rText }.Where(s => s.Length > 0));
            return $"{baseText} · 피부 컨디션: {conditions}";
        }

        private void UpdateCollections(PersonalColorType colorType)
        {
            string[] keywords;
            BeautyProductCard[] makeupItems;
            BeautyProductCard[] skinCareItems;

            switch (colorType)
            {
                case PersonalColorType.SpringWarm:
                    keywords = new[] { "코랄 포인트", "피치 블러셔", "웜 베이스", "화사한 마무리" };
                    makeupItems = new[]
                    {
                        new BeautyProductCard("코랄 무드 블러셔", "치크", "웜톤 혈색 보정에 적합한 소프트 코랄", "4.7", new[] { "톤매치 96%", "가루날림 적음", "데일리 추천" }),
                        new BeautyProductCard("살구빛 크림 립틴트", "립", "입술 생기와 얼굴 밝기 밸런스를 동시에 보정", "4.6", new[] { "착색 자연스러움", "건조함 적음", "봄웜 베스트" }),
                        new BeautyProductCard("피치 아이 팔레트 4구", "아이", "노란기 도는 피부에 음영 대비가 과하지 않음", "4.5", new[] { "초보자 사용 쉬움", "밀착력 우수", "펄 날림 적음" }),
                    };
                    skinCareItems = new[]
                    {
                        new BeautyProductCard("시카 진정 토너 패드", "진정", "열감과 홍조가 올라오는 날 빠르게 진정", "4.8", new[] { "민감피부 적합", "저자극", "흡수 빠름" }),
                        new BeautyProductCard("장벽 강화 세라마이드 크림", "보습", "수분 손실을 줄이고 메이크업 들뜸 예방", "4.7", new[] { "수분막 유지", "밤/낮 겸용", "속건조 개선" }),
                        new BeautyProductCard("광채 세럼 쿠션 21N", "베이스", "밝기 보정 + 웜베이지 밀착 매칭", "4.5", new[] { "웜베이지 매칭", "광채 마감", "밀착력 우수" }),
                    };
                    break;

                case PersonalColorType.SummerCool:
                    keywords = new[] { "라벤더 블러셔", "쿨핑크 립", "블루 베이스", "청량한 마무리" };
                    makeupItems = new[]
                    {
                        new BeautyProductCard("라벤더 쉬머 블러셔", "치크", "쿨톤 피부에 자연스러운 혈색과 투명감 부여", "4.7", new[] { "쿨톤 매칭", "가루날림 적음", "청량한 발색" }),
                        new BeautyProductCard("로즈 틴트", "립", "쿨핑크 발색으로 여름쿨 피부톤과 조화", "4.6", new[] { "착색 자연스러움", "쿨 로즈", "데일리 추천" }),
                        new BeautyProductCard("블루 베이스 파운데이션", "베이스", "쿨베이지 언더톤으로 피부 투명도 향상", "4.5", new[] { "쿨베이지 매칭", "투명 마감", "밀착력 우수" }),
                    };
                    skinCareItems = new[]
                    {
                        new BeautyProductCard("히알루론산 수분 크림", "보습", "피부 수분 밀도를 높이고 쿨톤 피부 광채 유지", "4.8", new[] { "수분 충전", "쿨톤 적합", "흡수 빠름" }),
                        new BeautyProductCard("나이아신아마이드 세럼", "미백", "쿨톤 피부 미백 관리 + 피부결 정리", "4.7", new[] { "미백 기능성", "쿨톤 추천", "꾸준히 사용" }),
                        new BeautyProductCard("약산성 수분 클렌저", "클렌징", "피부 pH 밸런스 유지로 트러블 유발 감소", "4.5", new[] { "세정력 균형", "당김 적음", "아침 세안 추천" }),
                    };
                    break;

                case PersonalColorType.AutumnWarm:
                    keywords = new[] { "브라운 블러셔", "버건디 립", "카키 아이", "성숙한 마무리" };
                    makeupItems = new[]
                    {
                        new BeautyProductCard("브라운 무드 블러셔", "치크", "테라코타 발색으로 가을웜 피부에 깊이감 부여", "4.7", new[] { "테라코타", "가을웜 베스트", "데일리 추천" }),
                        new BeautyProductCard("뮤트 버건디 립", "립", "차분한 버건디로 가을웜 피부 분위기 완성", "4.6", new[] { "뮤트 발색", "가을웜 어울림", "착색 자연스러움" }),
                        new BeautyProductCard("머스타드 아이 팔레트", "아이", "카키/머스타드 음영으로 차분한 눈매 연출", "4.5", new[] { "웜 어스톤", "밀착력 우수", "펄 날림 적음" }),
                    };
                    skinCareItems = new[]
                    {
                        new BeautyProductCard("시카 진정 토너 패드", "진정", "열감과 홍조가 올라오는 날 빠르게 진정", "4.8", new[] { "민감피부 적합", "저자극", "흡수 빠름" }),
                        new BeautyProductCard("장벽 강화 세라마이드 크림", "보습", "수분 손실을 줄이고 메이크업 들뜸 예방", "4.7", new[] { "수분막 유지", "밤/낮 겸용", "속건조 개선" }),
                        new BeautyProductCard("약산성 수분 클렌저", "클렌징", "피부 pH 밸런스 유지로 트러블 유발 감소", "4.5", new[] { "세정력 균형", "당김 적음", "아침 세안 추천" }),
                    };
                    break;

                default: // WinterCool
                    keywords = new[] { "로즈 블러셔", "레드 립", "쿨 베이스", "또렷한 마무리" };
                    makeupItems = new[]
                    {
                        new BeautyProductCard("쿨 로즈 블러셔", "치크", "겨울쿨 피부에 선명하고 또렷한 혈색 부여", "4.7", new[] { "쿨 로즈", "겨울쿨 베스트", "또렷한 발색" }),
                        new BeautyProductCard("선명한 레드 립", "립", "강렬한 레드 발색으로 겨울쿨 포인트 완성", "4.6", new[] { "선명 레드", "쿨 어울림", "고채도 발색" }),
                        new BeautyProductCard("블루 언더톤 파운데이션", "베이스", "쿨 언더톤 매칭으로 피부 밝기와 투명감 향상", "4.5", new[] { "쿨베이지", "블루 언더톤", "밀착력 우수" }),
                    };
                    skinCareItems = new[]
                    {
                        new BeautyProductCard("히알루론산 수분 크림", "보습", "피부 수분 밀도를 높이고 쿨톤 피부 광채 유지", "4.8", new[] { "수분 충전", "쿨톤 적합", "흡수 빠름" }),
                        new BeautyProductCard("나이아신아마이드 세럼", "미백", "쿨톤 피부 미백 관리 + 피부결 정리", "4.7", new[] { "미백 기능성", "쿨톤 추천", "꾸준히 사용" }),
                        new BeautyProductCard("약산성 수분 클렌저", "클렌징", "피부 pH 밸런스 유지로 트러블 유발 감소", "4.5", new[] { "세정력 균형", "당김 적음", "아침 세안 추천" }),
                    };
                    break;
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                FocusKeywords.Clear();
                foreach (var k in keywords) FocusKeywords.Add(k);

                if (!_hasAiRecommendations)
                {
                    MakeupItems.Clear();
                    foreach (var m in makeupItems) MakeupItems.Add(m);

                    SkinCareItems.Clear();
                    foreach (var s in skinCareItems) SkinCareItems.Add(s);
                }
            });
        }
    }

    public sealed class BeautyProductCard
    {
        public string Name { get; }
        public string Category { get; }
        public string RecommendationReason { get; }
        public string Rating { get; }
        public IReadOnlyList<string> Tags { get; }
        public string ProductUrl { get; }
        public bool HasUrl => !string.IsNullOrEmpty(ProductUrl);

        public BeautyProductCard(string name, string category, string recommendationReason, string rating, IReadOnlyList<string> tags, string productUrl = "")
        {
            Name = name;
            Category = category;
            RecommendationReason = recommendationReason;
            Rating = rating;
            Tags = tags;
            ProductUrl = productUrl;
        }
    }
}
