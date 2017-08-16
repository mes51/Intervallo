using Intervallo.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace Intervallo.Form
{
    /// <summary>
    /// AboutWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            VersionTextBlock.Text = typeof(AboutWindow).Assembly.GetName().Version.ToString();
            CopyrightTextBlock.Text = GetCopyright();
        }

        void LicenseTextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new LicenseWindow(LangResources.AboutWindow_License, string.Format(LicenseResources.License_Intervallo, GetCopyright())).ShowDialog();
        }

        void LibraryLicenseTextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new LicenseWindow(LangResources.AboutWindow_LibraryLicense, LicenseResources.License_Libraries).ShowDialog();
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        static string GetCopyright()
        {
            return ((AssemblyCopyrightAttribute)typeof(AboutWindow).Assembly.GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright;
        }
    }
}
