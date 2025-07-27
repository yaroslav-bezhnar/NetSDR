using System.Windows;
using System.Windows.Input;

namespace NetSDR.Wpf.Views;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    #region constructors

    public MainWindow()
    {
        InitializeComponent();
    }

    #endregion

    #region methods

    private void CloseWindow(object sender, RoutedEventArgs e) => Close();

    private void DragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    #endregion
}