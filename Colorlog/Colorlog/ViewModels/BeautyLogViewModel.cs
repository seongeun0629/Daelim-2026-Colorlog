using Colorlog.Services;
using Colorlog.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Colorlog.ViewModels
{
    public partial class BeautyLogViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private int _userId;

        [ObservableProperty] private string _userDisplayName = "사용자";
        [ObservableProperty] private string _toneSummary = "-";
        [ObservableProperty] private string _skinConditionSummary = "-";

        public ObservableCollection<string> FocusKeywords { get; } = new();
        public ObservableCollection<BeautyProductCard> MakeupItems { get; } = new();
        public ObservableCollection<BeautyProductCard> SkinCareItems { get; } = new();


        public BeautyLogViewModel(DatabaseService databaseService, int userId)
        {
            _databaseService = databaseService;
            _userId = userId;
            LoadFromDatabase();
        }

        private void LoadFromDatabase()
        {
            try
            {
                // 유저 이름
                var user = _databaseService.GetUserById(_userId);
                if (user != null)
                    UserDisplayName = user.UserName;

                // 최근 진단 요약
                var diagnosis = _databaseService.GetLatestDiagnosis(_userId);
                if (diagnosis != null)
                {
                    ToneSummary = string.IsNullOrEmpty(diagnosis.PersonalColorName)
                        ? "-" : diagnosis.PersonalColorName;

                    var oilyText = diagnosis.OilyStatus switch
                    {
                        "Oily" => "유분 많음",
                        "Normal" => "유분 정상",
                        "Not Oily" => "건조",
                        _ => "-"
                    };
                    SkinConditionSummary =
                        $"밝기 {diagnosis.Brightness} · 붉은기 {diagnosis.Redness} · {oilyText}";
                }

                // 추천 제품 DB에서 로드
                var recs = _databaseService.GetLatestRecommendations(_userId);

                MakeupItems.Clear();
                SkinCareItems.Clear();

                var makeupCats = new HashSet<string> { "치크", "립", "아이", "베이스" };

                if (recs.Count > 0)
                {
                    foreach (var rec in recs)
                    {
                        var card = new BeautyProductCard(
                            rec.ProductName,
                            rec.Category,
                            rec.RecReason,
                            "-",
                            new[] { rec.Category }
                        );

                        if (makeupCats.Contains(rec.Category))
                            MakeupItems.Add(card);
                        else
                            SkinCareItems.Add(card);
                    }
                }
                else
                {
                    MakeupItems.Add(new BeautyProductCard(
                        "진단 후 추천 제품이 표시됩니다", "-",
                        "실시간 분석을 먼저 진행해주세요.", "-", new[] { "" }));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BeautyLog] LoadFromDatabase 오류: {ex.Message}");
            }
        }

        public void UpdateUserId(int userId)
        {
            _userId = userId;
            LoadFromDatabase();
        }
    }

    public sealed class BeautyProductCard
    {
        public string Name { get; }
        public string Category { get; }
        public string RecommendationReason { get; }
        public string Rating { get; }
        public IReadOnlyList<string> Tags { get; }

        public BeautyProductCard(string name, string category, string recommendationReason,
            string rating, IReadOnlyList<string> tags)
        {
            Name = name;
            Category = category;
            RecommendationReason = recommendationReason;
            Rating = rating;
            Tags = tags;
        }
    }
}



