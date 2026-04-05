using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Colorlog.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
        }
        bool isSidebarOpen = false;
        private void BtnToggle_Click(object sender, RoutedEventArgs e)
        {
            if (isSidebarOpen)
            {
                ((Storyboard)this.FindResource("CloseSidebar")).Begin();
            }
            else
            {
                ((Storyboard)this.FindResource("OpenSidebar")).Begin();
            }
            isSidebarOpen = !isSidebarOpen; 
        }

        private void exitProgram(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}