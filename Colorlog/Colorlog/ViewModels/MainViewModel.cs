using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Colorlog.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty] 
        private object _currentView;

        [ObservableProperty]
        private ListBoxItem? _selectedMenuItem;

        public IRelayCommand ShowDashboardCommand { get; }
        public IRelayCommand ShowLiveAnalysisCommand { get; }
        public IRelayCommand ShowHistoryCommand { get; }
        public IRelayCommand ShowBeautyLogCommand { get; }
        public IRelayCommand ShowSettingsCommand { get; }

        private readonly DashboardViewModel _dashboardViewModel;
        private readonly LiveAnalysisViewModel _liveAnalysisViewModel;
        private readonly HistoryViewModel _historyViewModel;
        private readonly BeautyLogViewModel _beautyLogViewModel;
        private readonly SettingsViewModel _settingsViewModel;

        public MainViewModel()
        {
            _dashboardViewModel = new DashboardViewModel();
            _liveAnalysisViewModel = new LiveAnalysisViewModel();
            _historyViewModel = new HistoryViewModel();
            _beautyLogViewModel = new BeautyLogViewModel();
            _settingsViewModel = new SettingsViewModel();

            ShowDashboardCommand = new RelayCommand(() => CurrentView = _dashboardViewModel);
            ShowLiveAnalysisCommand = new RelayCommand(() => CurrentView = _liveAnalysisViewModel);
            ShowHistoryCommand = new RelayCommand(() => CurrentView = _historyViewModel);
            ShowBeautyLogCommand = new RelayCommand(() => CurrentView = _beautyLogViewModel);
            ShowSettingsCommand = new RelayCommand(() => CurrentView = _settingsViewModel);

            CurrentView = _dashboardViewModel;
        }

        partial void OnSelectedMenuItemChanged(ListBoxItem? value)
        {
            if(value==null || value.Tag == null) return;
            string tag = value.Tag.ToString()!;

            CurrentView = tag switch
            {
                "Dashboard" => _dashboardViewModel,
                "LiveAnalysis" => _liveAnalysisViewModel,
                "History" => _historyViewModel,
                "BeautyLog" => _beautyLogViewModel,
                "Settings" => _settingsViewModel,
                _ => CurrentView
            };
        }

    }
}
