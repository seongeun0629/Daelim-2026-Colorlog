using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colorlog.Models;
using Colorlog.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;


namespace Colorlog.ViewModels
{
    public partial class ProfileSelectViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;

        public ObservableCollection<User> Profiles { get; } = new();

        [ObservableProperty] private User? selectedProfile;

        public Action<User>? ProfileSelected { get; set; }
        public Action? AddNewProfileRequested { get; set; }

        public ProfileSelectViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            LoadProfiles();
        }

        private void LoadProfiles()
        {
            Profiles.Clear();
            var users = _databaseService.GetAllUsers();
            foreach (var user in users)
            {
                Profiles.Add(user);
            }
        }

        [RelayCommand]
        private void SelectProfile(User user)
        {
            ProfileSelected?.Invoke(user);
        }

        [RelayCommand]
        private void AddNewProfile()
        {
            AddNewProfileRequested?.Invoke();
        }
    }
}
