using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Colorlog.Services;

namespace Colorlog.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object? _currentView;

        [ObservableProperty]
        private string _selectedMenuTag = "Dashboard";

        public DashboardViewModel DashboardViewModel { get; }
        public LiveAnalysisViewModel LiveAnalysisViewModel { get; }
        public HistoryViewModel HistoryViewModel { get; }
        public BeautyLogViewModel BeautyLogViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }

        [ObservableProperty]
        private bool _isSidebarExpanded = true;

        public MainViewModel()
        {
            var pythonService = new PythonEngineService();

            // SettingsViewModel을 먼저 만들어서 Dashboard와 공유
            SettingsViewModel = new SettingsViewModel(pythonService);
            DashboardViewModel = new DashboardViewModel(SettingsViewModel);
            LiveAnalysisViewModel = new LiveAnalysisViewModel(pythonService, SettingsViewModel);
            HistoryViewModel = new HistoryViewModel(pythonService);
            BeautyLogViewModel = new BeautyLogViewModel();

            UpdateView();

            _ = InitializeAsync(pythonService);
        }

        private async Task InitializeAsync(PythonEngineService pythonService)
        {
            // 진단 완료 이벤트 → History 리로드 + Settings 통계 갱신
            pythonService.OnDiagnosisSaved += (json) =>
            {
                _ = HistoryViewModel.LoadHistoryAsync();
                _ = SettingsViewModel.LoadUserStatsAsync();
            };

            // DB에서 가장 최근 사용자 조회 → user_id 직접 설정 (이름 기반 재조회 불필요)
            var existingStats = await pythonService.QueryUserStatsAsync();
            var dbUserId = existingStats["user_id"]?.ToObject<int>() ?? -1;
            var dbUserName = existingStats["user_name"]?.ToString();

            if (dbUserId >= 0)
            {
                // 기존 사용자: user_id를 직접 설정하고 이름만 복원 (age 덮어쓰기 없음)
                pythonService.CurrentUserId = dbUserId;
                if (!string.IsNullOrEmpty(dbUserName))
                    SettingsViewModel.UserName = dbUserName;
            }
            else
            {
                // 첫 실행: 기본 이름으로 신규 사용자 생성
                await pythonService.SaveUserAsync(SettingsViewModel.UserName);
            }

            // 히스토리 및 통계 로드
            await HistoryViewModel.LoadHistoryAsync();
            await SettingsViewModel.LoadUserStatsAsync();
        }

        partial void OnSelectedMenuTagChanged(string value)
        {
            UpdateView();
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
        private void ToggleSidebar()
        {
            IsSidebarExpanded = !IsSidebarExpanded;
        }
    }
}
