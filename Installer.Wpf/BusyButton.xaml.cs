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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Intaller.Wpf
{
    /// <summary>
    /// Interaction logic for BusyButton.xaml
    /// </summary>
    public partial class BusyButton : UserControl
    {
        public BusyButton()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
            "IsBusy", typeof(bool), typeof(BusyButton), new PropertyMetadata(default(bool)));

        public bool IsBusy
        {
            get { return (bool) GetValue(IsBusyProperty); }
            set { SetValue(IsBusyProperty, value); }
        }

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(
            "Image", typeof(ImageSource), typeof(BusyButton), new PropertyMetadata(default(ImageSource)));

        public ImageSource Image
        {
            get { return (ImageSource) GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public static readonly DependencyProperty ButtonContentProperty = DependencyProperty.Register(
            "ButtonContent", typeof(object), typeof(BusyButton), new PropertyMetadata(default(object)));

        public object ButtonContent
        {
            get { return (object) GetValue(ButtonContentProperty); }
            set { SetValue(ButtonContentProperty, value); }
        }

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(ICommand), typeof(BusyButton), new PropertyMetadata(default(ICommand)));

        public ICommand Command
        {
            get { return (ICommand) GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }
    }
}
