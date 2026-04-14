using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Colorlog.Views.Pages
{
    public partial class SettingsView : UserControl
    {
        private const double CompactLayoutBreakpointWidth = 920;

        public SettingsView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyCardLayout(ActualWidth);
            UpdateWebcamPreviewClip();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyCardLayout(e.NewSize.Width);
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

        private void ApplyCardLayout(double width)
        {
            if (CardsLayoutGrid is null
                || UserInfoCard is null
                || CameraSettingsCard is null
                || PreferenceCard is null
                || DataManagementCard is null)
            {
                return;
            }

            if (width <= 0)
            {
                return;
            }

            bool compact = width < CompactLayoutBreakpointWidth;

            var grid = CardsLayoutGrid;
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();

            var cardMarginNormal = new Thickness(10);
            var cardMarginStacked = new Thickness(10, 10, 10, 20);
            var cardMarginLast = new Thickness(10);

            if (compact)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                for (var i = 0; i < 4; i++)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }

                Grid.SetColumn(UserInfoCard, 0);
                Grid.SetRow(UserInfoCard, 0);
                Grid.SetColumn(CameraSettingsCard, 0);
                Grid.SetRow(CameraSettingsCard, 1);
                Grid.SetColumn(PreferenceCard, 0);
                Grid.SetRow(PreferenceCard, 2);
                Grid.SetColumn(DataManagementCard, 0);
                Grid.SetRow(DataManagementCard, 3);

                UserInfoCard.Margin = cardMarginStacked;
                CameraSettingsCard.Margin = cardMarginStacked;
                PreferenceCard.Margin = cardMarginStacked;
                DataManagementCard.Margin = cardMarginLast;
            }
            else
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid.SetColumn(UserInfoCard, 0);
                Grid.SetRow(UserInfoCard, 0);
                Grid.SetColumn(CameraSettingsCard, 2);
                Grid.SetRow(CameraSettingsCard, 0);
                Grid.SetColumn(PreferenceCard, 0);
                Grid.SetRow(PreferenceCard, 2);
                Grid.SetColumn(DataManagementCard, 2);
                Grid.SetRow(DataManagementCard, 2);

                UserInfoCard.Margin = cardMarginNormal;
                CameraSettingsCard.Margin = cardMarginNormal;
                PreferenceCard.Margin = cardMarginNormal;
                DataManagementCard.Margin = cardMarginNormal;
            }
        }
    }
}
