using Colorlog.Platform;
using Colorlog.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Colorlog.Views
{
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            WindowBackdropHelper.TryApplyMica(this);
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(ViewModels.MainViewModel.SelectedMenuTag))
                    {
                        Dispatcher.BeginInvoke(PlayPageTransition, System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                };
            }
        }

        private void PlayPageTransition()
        {
            if (ContentArea.Content is not FrameworkElement page)
            {
                return;
            }

            page.Opacity = 0;
            page.RenderTransform = new System.Windows.Media.TranslateTransform(0, 6);

            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var slide = new DoubleAnimation(6, 0, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            page.BeginAnimation(OpacityProperty, fade);
            if (page.RenderTransform is System.Windows.Media.TranslateTransform tt)
            {
                tt.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slide);
            }
        }

        private void exitProgram(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (DataContext is MainViewModel vm)
                vm.Dispose();
            Application.Current.Shutdown();
        }
    }
}
