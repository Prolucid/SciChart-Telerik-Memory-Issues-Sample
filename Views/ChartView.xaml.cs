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
            var xDimension = 10000;
            var yDimension = 10000;
            
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.WriteLine($"{timestamp} Generating random data...");
            var randomGenerator = new Random();
            var zValues = new short[xDimension];
            
            for (var i = 0; i < xDimension; i++)
            {
                zValues[i] = (short)randomGenerator.Next(0, 101);
            }
            
            var data = new short[xDimension, yDimension];
            
            for (var x = 0; x < xDimension; x++)
            {
                for (var y = 0; y < yDimension; y++)
                {
                    data[x, y] = zValues[x];
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
