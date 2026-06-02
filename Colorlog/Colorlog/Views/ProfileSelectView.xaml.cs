using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Colorlog.Views
{
    /// <summary>
    /// ProfileSelectView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ProfileSelectView : Window
    {
        public ProfileSelectView()
        {
            InitializeComponent();
        }

        // 드래그 이동
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        // 닫기 버튼
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
