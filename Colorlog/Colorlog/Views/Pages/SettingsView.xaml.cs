using Colorlog.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Colorlog.Views.Pages;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.InitializeCamera();
        }
    }
    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.StopCamera();
        }
    }
    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateWebcamPreviewClip();
    }

    private void PreviewHost_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateWebcamPreviewClip();
    }

    private void UpdateWebcamPreviewClip()
    {
        if (PreviewHost is null || WebcamPreviewImage is null)
        {
            return;
        }

        var w = Math.Max(1, PreviewHost.ActualWidth);
        var h = Math.Max(1, PreviewHost.ActualHeight);
        WebcamPreviewImage.Clip = new RectangleGeometry(new Rect(0, 0, w, h), 12, 12);
    }
}
