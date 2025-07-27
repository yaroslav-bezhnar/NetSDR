using System.Windows;
using System.Windows.Input;

namespace NetSDR.Wpf.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        //private bool isDarkTheme = false;

        //private void ToggleTheme(object sender, RoutedEventArgs e)
        //{
        //    isDarkTheme = !isDarkTheme;
        //    ThemeIcon.Text = isDarkTheme ? "🌙" : "☀";

        //    var themePath = isDarkTheme ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";
        //    var newTheme = new ResourceDictionary { Source = new Uri(themePath, UriKind.Relative) };

        //    Application.Current.Resources.MergedDictionaries.Clear();
        //    Application.Current.Resources.MergedDictionaries.Add(newTheme);
        //}
    }
}