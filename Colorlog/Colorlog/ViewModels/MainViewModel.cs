using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
            DashboardViewModel = new DashboardViewModel();
            LiveAnalysisViewModel = new LiveAnalysisViewModel();
            HistoryViewModel = new HistoryViewModel();
            BeautyLogViewModel = new BeautyLogViewModel();
            SettingsViewModel = new SettingsViewModel();

            UpdateView();
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
