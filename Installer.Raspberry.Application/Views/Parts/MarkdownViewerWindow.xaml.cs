using System.Windows;

namespace Installer.Raspberry.Application.Views.Parts
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
