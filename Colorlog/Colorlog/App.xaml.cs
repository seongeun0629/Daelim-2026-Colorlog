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

            var viewModel = new MainViewModel();
            var view = new MainView();

            view.DataContext = viewModel;

            view.Show();
        }
    }

}
