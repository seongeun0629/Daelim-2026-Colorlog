using Colorlog.Models;
using Colorlog.Services;
using Colorlog.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Colorlog.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private string userName = "-";

    [ObservableProperty] private string userAge = string.Empty;

    [ObservableProperty] private DateTime userBirthDate = DateTime.Today.AddYears(-24);

    [ObservableProperty] private string userGender = "선택 안함";

    [ObservableProperty] private bool isGenderVisible;

    [ObservableProperty] private string personalColorName = "진단 미실시";

    [ObservableProperty] private string diagnosisCount = "0회";

    [ObservableProperty] private string joinDate = "2026.01.03";

    private readonly Services.DatabaseService _databaseService;

    private readonly PythonEngineService _pythonService;
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

    private int _currentUserId;

    [ObservableProperty]
    private string preferenceSelectionSummary = "아직 선택된 항목이 없습니다.";

    public ObservableCollection<SelectablePreferenceItem> MoodItems { get; } = new();
    public ObservableCollection<SelectablePreferenceItem> SkinItems { get; } = new();
    public ObservableCollection<SelectablePreferenceItem> ToneItems { get; } = new();

    public SettingsViewModel(Services.DatabaseService databaseService, int userId, PythonEngineService pythonService)
    {
        _databaseService = databaseService;
        _currentUserId = userId;
        _pythonService = pythonService;

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
        DiscoverCameraDevices();
        SyncCameraAvailability();
        SyncUserAgeFromBirthDate();

        try
        {
            var user = _databaseService.GetUserById(userId);
            if (user != null)
            {
                UserName = user.UserName;
                UserGender = user.Gender ?? "선택 안함";
                if (!string.IsNullOrWhiteSpace(user.Age))
                    UserAge = user.Age;
                if (!string.IsNullOrEmpty(user.ProfileImagePath)
                    && File.Exists(user.ProfileImagePath))
                    ProfileImagePath = user.ProfileImagePath;
            }

            var stats = _databaseService.GetUserStats(userId);
            if (stats != null)
            {
                PersonalColorName = string.IsNullOrEmpty(stats.LatestColorType)
                    ? "진단 미실시" : stats.LatestColorType;
                DiagnosisCount = $"{stats.DiagnosisCount}회";
                JoinDate = stats.JoinDate;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"초기 유저 정보 로드 실패: {ex.Message}");
        }

        //추구미 불러오기
        var savedStyle = _databaseService.GetPreferredStyle(userId);
        if (!string.IsNullOrEmpty(savedStyle))
        {
            var savedItems = savedStyle.Split(',').Select(s => s.Trim()).ToHashSet();
            foreach (var item in MoodItems.Concat(SkinItems).Concat(ToneItems))
            {
                if (savedItems.Contains(item.Label))
                    item.IsSelected = true;
            }
        }
    }

    [RelayCommand]
    private async Task RegenRecommendations()
    {
        _databaseService.UpdatePreferredStyle(_currentUserId,
            string.Join(", ", MoodItems.Concat(SkinItems).Concat(ToneItems)
                .Where(x => x.IsSelected).Select(x => x.Label)));

        var engineDir = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            @"..\..\..\..\ColorLog_Engine"));

        var pythonPath = FindPythonPath();  // ✅ 동적으로 찾기

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"regen_recs.py --user-id {_currentUserId}",
            WorkingDirectory = engineDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = System.Diagnostics.Process.Start(psi);
        if (process != null)
        {
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            Debug.WriteLine($"[RegenRecs] output: {output}");
            Debug.WriteLine($"[RegenRecs] error: {error}");
        }

        WeakReferenceMessenger.Default.Send(new ProfileSwitchedMessage(_currentUserId));

        MessageBox.Show("추천이 업데이트됐습니다! 뷰티 로그를 확인해보세요.",
            "추천 업데이트 완료", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private string FindPythonPath()
    {
        var username = Environment.UserName;
        var candidates = new[]
        {
        $@"C:\Users\{username}\anaconda3\envs\colorlog\python.exe",
        $@"C:\Users\{username}\Anaconda3\envs\colorlog\python.exe",
        $@"C:\ProgramData\anaconda3\envs\colorlog\python.exe",
        $@"C:\ProgramData\Anaconda3\envs\colorlog\python.exe",
    };

        foreach (var candidate in candidates)
        {
            if (System.IO.File.Exists(candidate))
                return candidate;
        }
        return "python";
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
                
                if(_currentUserId > 0)
                {
                    _databaseService.UpdateUserProfileImage(_currentUserId, targetFile);
                }
                MessageBox.Show("프로필 사진이 성공적으로 변경되었습니다!", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이미지를 불러오는 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        
    }

    private void OnCameraNamesChanged(object? sender, NotifyCollectionChangedEventArgs e) => SyncCameraAvailability();

    private void SyncCameraAvailability()
    {
        HasCameras = CameraNames.Count > 0;
        if (!HasCameras)
        {
            SelectedCameraName = null;
            StopCamera();
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

        //DB 저장
        if (_currentUserId > 0 && picks.Count > 0)
            _databaseService.UpdatePreferredStyle(_currentUserId, string.Join(", ", picks));
    }

    [RelayCommand]
    private void ResetAllRecords()
    {
        var result = MessageBox.Show(
        "진단 기록과 추천 내역이 모두 삭제됩니다. 이 작업은 되돌릴 수 없습니다.\n계속하시겠습니까?",
        "기록 초기화",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            _databaseService.DeleteAllRecordsByUser(_currentUserId);
            MessageBox.Show("초기화를 완료했습니다.", "안내", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"초기화 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
        var currentUser = _databaseService.GetLatestUser();
        int? userId = currentUser?.UserId;

        var vm = new EditProfileViewModel(_databaseService, userId,  UserName, UserBirthDate);

        if (UserGender == "남") { vm.IsGenderMale = true; vm.IsGenderFemale = false; vm.IsGenderNone = false; }
        else if (UserGender == "여") { vm.IsGenderMale = false; vm.IsGenderFemale = true; vm.IsGenderNone = false; }
        else { vm.IsGenderNone = true; }

        var dialog = new EditProfileView(vm) { Owner = Application.Current.MainWindow };

        if (dialog.ShowDialog() != true || !vm.BirthDate.HasValue) return;

        UserName = vm.Name.Trim();
        UserBirthDate = vm.BirthDate.Value.Date;

        if (vm.IsGenderMale) UserGender = "남";
        else if (vm.IsGenderFemale) UserGender = "여";
        else UserGender = "선택 안 함";

        SyncUserAgeFromBirthDate();
    }

    [RelayCommand]
    private void SwitchProfile()
    {
        Debug.WriteLine("[SwitchProfile] 커맨드 실행됨");

        var profileVm = new ProfileSelectViewModel(_databaseService);
        var profileView = new Views.ProfileSelectView { DataContext = profileVm };

        profileVm.ProfileSelected = (selectedUser) =>
        {
            Debug.WriteLine($"[SwitchProfile] 프로필 선택됨: {selectedUser.UserId} - {selectedUser.UserName}");

            profileView.Close();
            WeakReferenceMessenger.Default.Send(new ProfileSwitchedMessage(selectedUser.UserId));
        };

        profileVm.AddNewProfileRequested = () =>
        {
            var editVm = new EditProfileViewModel(_databaseService, null, string.Empty, DateTime.Today.AddYears(-24));
            var editView = new Views.EditProfileView(editVm) { Owner = profileView };

            editVm.CloseRequested = (saved) =>
            {
                editView.DialogResult = saved;
                editView.Close();

                if (saved)
                {
                    var newUser = _databaseService.GetLatestUser();
                    if (newUser != null)
                    {
                        profileView.Close();
                        WeakReferenceMessenger.Default.Send(new ProfileSwitchedMessage(newUser.UserId));
                    }
                }
            };

            editView.ShowDialog();
        };

        profileView.ShowDialog();
    }
}
