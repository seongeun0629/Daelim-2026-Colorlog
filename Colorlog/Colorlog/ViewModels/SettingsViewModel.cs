using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Colorlog.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string userName = "유현성";

    [ObservableProperty]
    private string userAge = "24세";

    [ObservableProperty]
    private string personalColorName = "봄 웜 라이트";

    [ObservableProperty]
    private string diagnosisCount = "12회";

    [ObservableProperty]
    private string joinDate = "2026.01.03";

    public ObservableCollection<string> CameraNames { get; } = new();

    [ObservableProperty]
    private string? selectedCameraName;

    [ObservableProperty]
    private bool hasCameras;

    [ObservableProperty]
    private double brightness = 60;

    [ObservableProperty]
    private string preferenceSelectionSummary = "아직 선택된 항목이 없습니다.";

    public ObservableCollection<SelectablePreferenceItem> MoodItems { get; } = new();

    public ObservableCollection<SelectablePreferenceItem> SkinItems { get; } = new();

    public ObservableCollection<SelectablePreferenceItem> ToneItems { get; } = new();

    public SettingsViewModel()
    {
        foreach (var label in new[] { "청순", "화려", "스모키", "러블리", "시크", "데일리" })
        {
            MoodItems.Add(new SelectablePreferenceItem(label, RefreshPreferenceSummary));
        }

        foreach (var label in new[] { "글로우", "매트", "세미매트" })
        {
            SkinItems.Add(new SelectablePreferenceItem(label, RefreshPreferenceSummary));
        }

        foreach (var label in new[] { "웜톤", "쿨톤", "뉴트럴톤" })
        {
            ToneItems.Add(new SelectablePreferenceItem(label, RefreshPreferenceSummary));
        }

        CameraNames.CollectionChanged += OnCameraNamesChanged;
        SyncCameraAvailability();
    }

    private void OnCameraNamesChanged(object? sender, NotifyCollectionChangedEventArgs e) => SyncCameraAvailability();

    private void SyncCameraAvailability()
    {
        HasCameras = CameraNames.Count > 0;
        if (!HasCameras)
        {
            SelectedCameraName = null;
            return;
        }

        if (SelectedCameraName is null || !CameraNames.Contains(SelectedCameraName))
        {
            SelectedCameraName = CameraNames[0];
        }
    }

    private void RefreshPreferenceSummary()
    {
        var picks = MoodItems.Where(static x => x.IsSelected).Select(static x => x.Label)
            .Concat(SkinItems.Where(static x => x.IsSelected).Select(static x => x.Label))
            .Concat(ToneItems.Where(static x => x.IsSelected).Select(static x => x.Label))
            .ToList();

        PreferenceSelectionSummary = picks.Count == 0
            ? "아직 선택된 항목이 없습니다."
            : $"선택 {picks.Count}개: {string.Join(" · ", picks)}{Environment.NewLine}다음 분석·추천에 반영됩니다.";
    }

    [RelayCommand]
    private void ResetAllRecords()
    {
        var result = MessageBox.Show(
            "진단 기록과 추천 내역이 모두 삭제됩니다. 이 작업은 되돌릴 수 없습니다.\n계속하시겠습니까?",
            "기록 초기화",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _ = MessageBox.Show(
            "초기화를 완료했습니다.",
            "안내",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    private void ExportBackup()
    {
        _ = MessageBox.Show(
            "@@추후 연결하기@@",
            "데이터 백업",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
