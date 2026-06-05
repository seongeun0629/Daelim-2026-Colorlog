using System.Diagnostics;
using System.Windows.Controls;

namespace Colorlog.Views.Pages
{
    public partial class BeautyLogView : UserControl
    {
        public BeautyLogView()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
