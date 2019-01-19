using System.Windows;

namespace Installer.Lumia.Application.Views
{
    
    public partial class TextViewerWindow
    {
        public TextViewerWindow()
        {
            InitializeComponent();
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
