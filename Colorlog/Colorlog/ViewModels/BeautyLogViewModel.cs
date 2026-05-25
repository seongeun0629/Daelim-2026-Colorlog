using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Colorlog.ViewModels
{
    public partial class BeautyLogViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _userDisplayName = "사용자";

        [ObservableProperty]
        public string _toneSummary = "봄 웜 라이트 · 코랄/피치 계열 고채도 포인트 추천";

        [ObservableProperty]
        public string _skinConditionSummary = "오늘 피부 컨디션: 수분 보통 · 홍조 약간 · 밝기 양호";

        public ObservableCollection<string> FocusKeywords { get; }

        public ObservableCollection<BeautyProductCard> MakeupItems { get; }
        public ObservableCollection<BeautyProductCard> SkinCareItems { get; }

        public BeautyLogViewModel()
        {
            try
            {
                var dbService = new Colorlog.Services.DatabaseService();
                var latestUser = dbService.GetLatestUser();

                if (latestUser != null && !string.IsNullOrEmpty(latestUser.UserName))
                {
                    UserDisplayName = latestUser.UserName;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"대시보드 유저 이름 로드 실패: {ex.Message}");
            }

            FocusKeywords = new ObservableCollection<string>
            {
                "톤매치 우선",
                "홍조 보정",
                "저자극 성분",
                "지속력 중심"
            };

            MakeupItems = new ObservableCollection<BeautyProductCard>
            {
                new("코랄 무드 블러셔", "치크", "웜톤 혈색 보정에 적합한 소프트 코랄", "4.7", new[] { "톤매치 96%", "가루날림 적음", "데일리 추천" }),
                new("살구빛 크림 립틴트", "립", "입술 생기와 얼굴 밝기 밸런스를 동시에 보정", "4.6", new[] { "착색 자연스러움", "건조함 적음", "봄웜 베스트" }),
                new("피치 아이 팔레트 4구", "아이", "노란기 도는 피부에 음영 대비가 과하지 않음", "4.5", new[] { "초보자 사용 쉬움", "밀착력 우수", "펄 날림 적음" })
            };

            SkinCareItems = new ObservableCollection<BeautyProductCard>
            {
                new("시카 진정 토너 패드", "진정", "열감과 홍조가 올라오는 날 빠르게 진정", "4.8", new[] { "민감피부 적합", "저자극", "흡수 빠름" }),
                new("장벽 강화 세라마이드 크림", "보습", "수분 손실을 줄이고 메이크업 들뜸 예방", "4.7", new[] { "수분막 유지", "밤/낮 겸용", "속건조 개선" }),
                new("약산성 수분 클렌저", "클렌징", "피부 pH 밸런스 유지로 트러블 유발 감소", "4.5", new[] { "세정력 균형", "당김 적음", "아침 세안 추천" })
            };
        }
    }

    public sealed class BeautyProductCard
    {
        public string Name { get; }
        public string Category { get; }
        public string RecommendationReason { get; }
        public string Rating { get; }
        public IReadOnlyList<string> Tags { get; }

        public BeautyProductCard(string name, string category, string recommendationReason, string rating, IReadOnlyList<string> tags)
        {
            Name = name;
            Category = category;
            RecommendationReason = recommendationReason;
            Rating = rating;
            Tags = tags;
        }
    }
}
