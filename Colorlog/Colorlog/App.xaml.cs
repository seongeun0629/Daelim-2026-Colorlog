using Colorlog.ViewModels;
using Colorlog.Views;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Colorlog
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var databaseService = new Services.DatabaseService();
            var profileVm = new ViewModels.ProfileSelectViewModel(databaseService);
            var profileView = new Views.ProfileSelectView { DataContext = profileVm };


            profileVm.ProfileSelected = (selectedUser) =>
            {
                profileView.Close();
                OpenMainWindow(databaseService, selectedUser.UserId);
            };

            profileVm.AddNewProfileRequested = () =>
            {
                var editVm = new ViewModels.EditProfileViewModel(databaseService, null, string.Empty, DateTime.Today.AddYears(-24));
                var editView = new Views.EditProfileView(editVm) { Owner = profileView };

                editVm.CloseRequested = (saved) =>
                {
                    editView.DialogResult = saved;
                    editView.Close();

                    if (saved)
                    {
                        var newUser = databaseService.GetLatestUser();
                        if (newUser != null)
                        {
                            profileView.Close();
                            OpenMainWindow(databaseService, newUser.UserId);
                        }
                    }
                };

                editView.ShowDialog();
            };

            profileView.Show();
        }

        private void OpenMainWindow(Services.DatabaseService databaseService, int userId)
        {
            var viewModel = new ViewModels.MainViewModel(databaseService, userId);
            var view = new Views.MainView { DataContext = viewModel };
            view.Show();
        }
    }
}
