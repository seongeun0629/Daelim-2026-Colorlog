using Colorlog.Services;
using Colorlog.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        [ObservableProperty] private BeautyProductCard? _selectedCard;
        [ObservableProperty] private bool _isDetailVisible;

        public ObservableCollection<string> FocusKeywords { get; } = new();
        public ObservableCollection<BeautyProductCard> MakeupItems { get; } = new();
        public ObservableCollection<BeautyProductCard> SkinCareItems { get; } = new();

        [RelayCommand]
        private void ShowDetail(BeautyProductCard card)
        {
            SelectedCard = card;
            IsDetailVisible = true;
        }

        [RelayCommand]
        private void CloseDetail()
        {
            IsDetailVisible = false;
            SelectedCard = null;
        }

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
                Debug.WriteLine($"[BeautyLog] userId={_userId}, diagnosis={diagnosis?.PersonalColorName ?? "null"}");

                // FocusKeywords 업데이트
                FocusKeywords.Clear();
                if (diagnosis != null)
                {
                    ToneSummary = string.IsNullOrEmpty(diagnosis.PersonalColorName)
                        ? "진단 미실시" : diagnosis.PersonalColorName;

                    var oilyText = diagnosis.OilyStatus switch
                    {
                        "Oily" => "유분 많음",
                        "Possibly Oily" => "유분 약간",
                        "Normal" => "유분 정상",
                        "Not Oily" => "건조",
                        _ => "-"
                    };
                    SkinConditionSummary = $"밝기 {diagnosis.Brightness} · 붉은기 {diagnosis.Redness} · {oilyText}"; 

                    if (diagnosis.Brightness >= 70) FocusKeywords.Add("밝기 양호");

                    if (diagnosis.Brightness >= 70) FocusKeywords.Add("밝기 양호");
                    else FocusKeywords.Add("밝기 보정 필요");

                    if (diagnosis.Redness >= 60) FocusKeywords.Add("홍조 진정 필요");
                    else if (diagnosis.Redness >= 40) FocusKeywords.Add("홍조 약간");
                    else FocusKeywords.Add("홍조 없음");

                    var oilyKeyword = diagnosis.OilyStatus switch
                    {
                        "Oily" => "유분 관리 필요",
                        "Possibly Oily" => "유분 약간",
                        "Normal" => "유분 정상",
                        "Not Oily" => "수분 보충 필요",
                        _ => "피부 컨디션 분석 중"
                    };
                    FocusKeywords.Add(oilyKeyword);
                    FocusKeywords.Add("최근 30일 기반 추천");
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
                            rec.Rating,  
                            rec.ProductUrl,
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
                        "실시간 분석을 먼저 진행해주세요.", "-", 
                        "",
                        new[] { "" }));
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
        public string ProductUrl { get; }  
        public IReadOnlyList<string> Tags { get; }

        public BeautyProductCard(string name, string category, string recommendationReason,
            string rating, string productUrl, IReadOnlyList<string> tags)
        {
            Name = name;
            Category = category;
            RecommendationReason = recommendationReason;
            Rating = rating;
            ProductUrl = productUrl; 
            Tags = tags;
        }
    }
}



