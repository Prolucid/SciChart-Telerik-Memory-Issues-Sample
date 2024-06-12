using System.Windows;
using SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries;

namespace Views
{
    /// <summary>
    /// Interaction logic for ChartView.xaml
    /// </summary>
    public partial class ChartView
    {
        private readonly UniformHeatmapDataSeries<double, double, short>[] _graphData;
        private UniformHeatmapDataSeries<double, double, short> GraphData => _graphData[DisplayIndex];
        private int DisplayIndex { get; set; }

        private static UniformHeatmapDataSeries<double, double, short> GenerateRandomData()
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.WriteLine($"{timestamp} Generating random data...");
            var randomGenerator = new Random();
            var data = new short[10000, 10000];
            
            for (var i = 0; i < 10000; i++)
            {
                for (var j = 0; j < 10000; j++)
                {
                    data[i, j] = (short)randomGenerator.Next(0, 101);
                }
            }
            
            return new UniformHeatmapDataSeries<double, double, short>(data, 0, 1, 0, 1);
        }

        private void UpdateDisplayedFrameNumber()
        {
            FrameRun.Text = $"Displaying frame: {DisplayIndex}";
        }
        
        public ChartView()
        {
            InitializeComponent();
            
            _graphData = new UniformHeatmapDataSeries<double, double, short>[2];
            for (var i = 0; i < _graphData.Length; i++)
            {
                _graphData[i] = GenerateRandomData();
            }
            
            UpdateDisplayedFrameNumber();
            Heatmap.DataSeries = GraphData;
        }
        
        private void AdvanceFrame_Click(object sender, RoutedEventArgs e)
        {
            var oldIndex = DisplayIndex;
            DisplayIndex = (DisplayIndex + 1) % _graphData.Length;
            UpdateDisplayedFrameNumber();
            Heatmap.DataSeries = GraphData;
            // _graphData[oldIndex].Clear();
        }
        
        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            foreach (var g in _graphData)
            {
                g.Clear();
                g.ParentSurface?.Dispose();
            }

            Array.Clear(_graphData);
        }

        public void CleanUp()
        {
            foreach (var g in _graphData)
            {
                g.Clear();
                g.ParentSurface?.Dispose();
            }

            Array.Clear(_graphData);
        }
    }
}
