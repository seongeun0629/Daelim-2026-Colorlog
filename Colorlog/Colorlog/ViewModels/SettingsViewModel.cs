using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Colorlog.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.IO;

namespace Colorlog.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string userName = "유현성";

    [ObservableProperty]
    private string userAge = string.Empty;

    [ObservableProperty]
    private DateTime userBirthDate = DateTime.Today.AddYears(-24);

    [ObservableProperty]
    private string personalColorName = "봄 웜 라이트";

    [ObservableProperty]
    private string diagnosisCount = "12회";

    [ObservableProperty]
    private string joinDate = "2026.01.03";

    public ObservableCollection<string> CameraNames { get; } = new();

    public ICommand ChangeProfileImageCommand => new RelayCommand(ExecuteChangeProfileImage);

    [ObservableProperty]
    private string? selectedCameraName;

    [ObservableProperty]
    private bool hasCameras;

    [ObservableProperty]
    private double brightness = 60;

    [ObservableProperty]
    private bool isCameraLoading;

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
        SyncUserAgeFromBirthDate();
    }

    private void SyncUserAgeFromBirthDate()
    {
        UserAge = $"만 {EditProfileViewModel.GetCompletedAgeYears(UserBirthDate.Date, DateTime.Today)}세";
    }

    //사진 변경 명령 실행 메서드
    private string _profileImagePath = "pack://application:,,,/Assets/user.png"; 
    public string ProfileImagePath
    {
        get => _profileImagePath;
        set
        {
            _profileImagePath = value;
            OnPropertyChanged(nameof(ProfileImagePath)); 
        }
    }
    private void ExecuteChangeProfileImage()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Title = "프로필 사진 선택",
            Filter = "이미지 파일 (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) // 내 사진 폴더에서 시작
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                string sourceFile = openFileDialog.FileName;

                string targetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData", "Profiles");

                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                string fileName = $"profile_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(sourceFile)}";
                string targetFile = Path.Combine(targetDir, fileName);

                File.Copy(sourceFile, targetFile, true);

                ProfileImagePath = targetFile;

                MessageBox.Show("프로필 사진이 성공적으로 변경되었습니다!", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이미지를 불러오는 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    partial void OnSelectedCameraNameChanged(string? value)
    {
        if (!HasCameras || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _ = SimulateCameraWarmupAsync();
    }

    private async Task SimulateCameraWarmupAsync()
    {
        try
        {
            IsCameraLoading = true;
            await Task.Delay(500);
        }
        finally
        {
            IsCameraLoading = false;
        }
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

    [RelayCommand]
    private void EditProfile()
    {
        var vm = new EditProfileViewModel(UserName, UserBirthDate);
        var dialog = new EditProfileView(vm)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() != true || !vm.BirthDate.HasValue)
        {
            return;
        }

        UserName = vm.Name.Trim();
        UserBirthDate = vm.BirthDate.Value.Date;
        SyncUserAgeFromBirthDate();
    }
}
