using SciChart.Charting.Visuals;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ChartTabViewModel _chartTabVm;
        
        public MainWindow()
        {
            // You may add a scichart runtime license key here
            SciChartSurface.SetRuntimeLicenseKey("");
            InitializeComponent();
            _chartTabVm = new ChartTabViewModel();
            MainTabControl.ItemsSource = _chartTabVm.Tabs;
        }
        
        private void AddGraphTab_Click(object sender, RoutedEventArgs e)
        {
            _chartTabVm.AddTab();
        }

        private void X_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _chartTabVm.Tabs[MainTabControl.SelectedIndex].CleanUp();
            _chartTabVm.Tabs.RemoveAt(MainTabControl.SelectedIndex);
        }
    }
}
