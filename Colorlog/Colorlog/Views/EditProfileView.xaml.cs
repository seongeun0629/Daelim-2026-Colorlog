using System.Windows;
using System.Windows.Input;
using Colorlog.ViewModels;

namespace Colorlog.Views;

public partial class EditProfileView : Window
{
    public EditProfileView(EditProfileViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(bool dialogResult)
    {
        DialogResult = dialogResult;
        Close();
    }

    private void Chrome_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            try
            {
                DragMove();
            }
            catch
            {
                // ShowDialog 전 등 Owner 없이 DragMove 시 무시
            }
        }
    }
}
