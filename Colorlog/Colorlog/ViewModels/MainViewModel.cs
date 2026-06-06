using Colorlog.Models;
using Colorlog.Services;
using Colorlog.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace Colorlog.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        [ObservableProperty]
        private object? _currentView;

        [ObservableProperty]
        private string _selectedMenuTag = "Dashboard";
        

        [ObservableProperty]
        private bool _isSidebarExpanded = true;

        //UI에 바인딩할 현재 사용자 정보 프로퍼티
        [ObservableProperty]
        private UserStatsDto? _currentUserStats;

        [ObservableProperty]
        private string _personalColorResult = "진단 대기 중...";

        public DashboardViewModel DashboardViewModel { get; }
        public LiveAnalysisViewModel LiveAnalysisViewModel { get; }
        public HistoryViewModel HistoryViewModel { get; }
        public BeautyLogViewModel BeautyLogViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }

        private readonly PythonEngineService _engineService;
        
        public MainViewModel(Services.DatabaseService databaseService, int userId)
        {
            

            //var databaseService = new Services.DatabaseService();

            DashboardViewModel = new DashboardViewModel(databaseService, userId);
            SettingsViewModel = new SettingsViewModel(databaseService, userId);
            LiveAnalysisViewModel = new LiveAnalysisViewModel(new PythonEngineService(), SettingsViewModel)
            {
                CurrentUserId = userId
            };
            HistoryViewModel = new HistoryViewModel(databaseService, userId);
            BeautyLogViewModel = new BeautyLogViewModel(databaseService, userId);

            UpdateView();

            _engineService = new PythonEngineService();
            _engineService.OnColorDetected += (result) =>
            {
                PersonalColorResult = $"진단 결과: {result}";
            };

            _ = LoadUserStatsAsync(userId);

            WeakReferenceMessenger.Default.Register<ProfileSwitchedMessage>(this, (r, m) =>
            {
                var newUserId = m.Value;
                LiveAnalysisViewModel.CurrentUserId = newUserId;
                HistoryViewModel.UpdateUserId(newUserId);
                DashboardViewModel.UpdateUserId(newUserId);
                _ = LoadUserStatsAsync(newUserId);
            });

            WeakReferenceMessenger.Default.Unregister<ProfileSwitchedMessage>(this);
            WeakReferenceMessenger.Default.Register<ProfileSwitchedMessage>(this, (r, m) =>
            {
                var newUserId = m.Value;
                Debug.WriteLine($"[MainViewModel] 프로필 전환 수신: {newUserId}");

                App.Current.Dispatcher.Invoke(() =>
                {
                    LiveAnalysisViewModel.CurrentUserId = newUserId;
                    HistoryViewModel.UpdateUserId(newUserId);
                    DashboardViewModel.UpdateUserId(newUserId);
                    _ = LoadUserStatsAsync(newUserId);
                });
            });
        }

        partial void OnSelectedMenuTagChanged(string value)
        {
            UpdateView();

            if (value == "Dashboard" || value == "Settings")
            {
                _ = LoadUserStatsAsync(CurrentUserStats?.UserId);
            }
        }

        private void UpdateView()
        {
            CurrentView = SelectedMenuTag switch
            {
                "Dashboard" => DashboardViewModel,
                "LiveAnalysis" => LiveAnalysisViewModel,
                "History" => HistoryViewModel,
                "BeautyLog" => BeautyLogViewModel,
                "Settings" => SettingsViewModel,
                _ => DashboardViewModel
            };
        }

        [RelayCommand]
        private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;

        // 1. 사용자 데이터 조회 (get_user_stats.py 연동)
        public async Task LoadUserStatsAsync(int? userId = null)
        {
            try
            {
                string scriptPath = "ColorLog_Engine/query_user_stats.py";

                string arguments = userId.HasValue ? $"--user-id {userId.Value}" : "";

                ProcessStartInfo startInfo = CreateProcessStartInfo(scriptPath, arguments);
                using (Process? process = Process.Start(startInfo))
                {
                    if (process == null) return;

                    string jsonOutput = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (!string.IsNullOrWhiteSpace(jsonOutput))
                    {
                        CurrentUserStats = JsonSerializer.Deserialize<UserStatsDto>(jsonOutput);

                        if (CurrentUserStats?.UserId is int uid)
                            LiveAnalysisViewModel.CurrentUserId = uid;

                        Debug.WriteLine($"[성공] {CurrentUserStats?.UserName}님의 통계 데이터를 불러왔습니다.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"파이썬 데이터 조회 중 에러 발생: {ex.Message}");
            }
        }

        //2. 사용자 진단 시작 (save_user.py -> main.py 연동)
        [RelayCommand]
        private async Task StartDiagnosisAsync()
        {
            try
            {
                PersonalColorResult = "사용자 동기화 중...";

                // 1) 임시 또는 UI에서 입력받은 유저 정보 (예시 데이터)
                string inputName = CurrentUserStats?.UserName ?? "새 사용자";
                string inputAge = CurrentUserStats?.JoinDate ?? "20대";
                int? currentId = CurrentUserStats?.UserId;

                // 2) save_user.py 실행하여 DB 싱크 및 확정 ID 추출
                string syncScript = "ColorLog_Engine/save_user.py";
                string syncArgs = $"--user-name \"{inputName}\" --age \"{inputAge}\"";
                if (currentId.HasValue) syncArgs += $" --user-id {currentId.Value}";

                int confirmedId = -1;
                using (Process? syncProcess = Process.Start(CreateProcessStartInfo(syncScript, syncArgs)))
                {
                    if (syncProcess != null)
                    {
                        string output = await syncProcess.StandardOutput.ReadToEndAsync();
                        await syncProcess.WaitForExitAsync();
                        int.TryParse(output.Trim(), out confirmedId);
                    }
                }

                // 3) 확정된 ID를 가지고 진짜 AI 카메라 메인 엔진 구동
                if (confirmedId != -1)
                {
                    PersonalColorResult = "AI 진단 엔진 구동 중...";


                    string mainScript = "ColorLog_Engine/main.py";
                    string mainArgs = $"--user-id {confirmedId} --user-name \"{inputName}\"";

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = $"{mainScript} {mainArgs}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
            }
            catch (Exception ex)
            {
                PersonalColorResult = "엔진 실행 실패";
                Debug.WriteLine($"진단 시작 중 에러: {ex.Message}");
            }
        }
        [RelayCommand]
        private async Task ResetUserDataAsync()
        {
            if (CurrentUserStats == null) return;

            try
            {
                string resetScript = "ColorLog_Engine/delete_records.py"; 
                string arguments = $"--user-id {CurrentUserStats.UserId}";

                using (Process? process = Process.Start(CreateProcessStartInfo(resetScript, arguments)))
                {
                    if (process == null) return;
                    string result = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (result.Trim() == "ok")
                    {
                        Debug.WriteLine("유저 데이터 및 진단 기록 초기화 완료!");
                        await LoadUserStatsAsync(CurrentUserStats.UserId);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"유저 초기화 실패: {ex.Message}");
            }
        }

        // 공통 ProcessStartInfo 생성기
        private ProcessStartInfo CreateProcessStartInfo(string scriptPath, string arguments)
        {
            return new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"{scriptPath} {arguments}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
        }

        public void Dispose()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
    }
}
