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

        private readonly PythonEngineService _engineService;
        private string _persnalColorResult = "진단 대기 중...";

        public string PersonalColorResult
        {
            get => _persnalColorResult;
            set { _persnalColorResult = value; OnPropertyChanged(); }
        }
        
        public MainViewModel()
        {
            DashboardViewModel = new DashboardViewModel();
            LiveAnalysisViewModel = new LiveAnalysisViewModel();
            HistoryViewModel = new HistoryViewModel();
            BeautyLogViewModel = new BeautyLogViewModel();
            SettingsViewModel = new SettingsViewModel();

            UpdateView();

            _engineService = new PythonEngineService();
            _engineService.OnColorDetected += (result) =>
            {
                PersonalColorResult = $"진단 결과: {result}";
            };

            _engineService.Start();
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
