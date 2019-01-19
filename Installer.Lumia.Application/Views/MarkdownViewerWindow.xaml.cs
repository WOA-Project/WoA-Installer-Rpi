using System.Windows;

namespace Installer.Lumia.Application.Views
{
    
    public partial class MarkdownViewerWindow
    {
        public MarkdownViewerWindow()
        {
            InitializeComponent();
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
