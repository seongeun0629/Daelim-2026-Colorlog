using System.Windows.Controls;
using Colorlog.ViewModels;

namespace Colorlog.Views.Pages;

public partial class LiveAnalysisView : UserControl
{
    public LiveAnalysisView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is LiveAnalysisViewModel vm)
        {
            vm.InitializeCameraPreview();
        }
    }

    private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is LiveAnalysisViewModel vm)
        {
            vm.StopPage();
        }
    }
}
