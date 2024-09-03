using System.Windows;
using SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Charting.Visuals.Axes;
using SciChart.Drawing.Common;
using SciChart.Charting.Numerics.CoordinateCalculators;
using SciChart.Data.Model;
using SciChart.Charting2D.Interop;

namespace Views
{
    //public class CustomHeatmapDS : UniformHeatmapDataSeries<double, double, double>
    //{
    //    public CustomHeatmapDS(double[,] zValues, double xStart, double xStep, double yStart, double yStep, IPointMetadata[,] metadata)
    //        : base (zValues, xStart, xStep, yStart, yStep, metadata)
    //    {

    //    }

    //    public override double Get
    //}

    // create custom RenderableSeries that inherits from FastUniformHeatmapRenderableSeries and overrides InternalDraw

    /// <summary>
    /// Custom CoordinateCalculator for Wraparound
    /// </summary>
    public class WraparoundCoordinateCalculator : ICoordinateCalculator<double>
    {
        double mod(double x, double m)
        {
            return (x % m + m) % m;
        }
        public bool IsDiscontinuousAxisCalculator => fallbackCalculator.IsDiscontinuousAxisCalculator;

        public bool IsCategoryAxisCalculator => fallbackCalculator.IsCategoryAxisCalculator;

        public bool IsLogarithmicAxisCalculator => fallbackCalculator.IsLogarithmicAxisCalculator;

        public bool IsTernaryAxisCalculator => fallbackCalculator.IsTernaryAxisCalculator;

        public bool IsPolarAxisCalculator => fallbackCalculator.IsPolarAxisCalculator;

        public bool IsHorizontalAxisCalculator => fallbackCalculator.IsHorizontalAxisCalculator;

        public bool IsXAxisCalculator => fallbackCalculator.IsXAxisCalculator;

        public bool HasFlippedCoordinates => fallbackCalculator.HasFlippedCoordinates;

        public double CoordinatesOffset => fallbackCalculator.CoordinatesOffset;

        public bool CanSupportNativeCoordinateCalculation => fallbackCalculator.CanSupportNativeCoordinateCalculation;

        public double ViewportOffset => fallbackCalculator.ViewportOffset;

        public double Size => fallbackCalculator.Size;

        public double VisibleMin => fallbackCalculator.VisibleMin;

        public double VisibleMax => fallbackCalculator.VisibleMax;

        private ICoordinateCalculator<double> fallbackCalculator;

        public WraparoundCoordinateCalculator(AxisParams axisParams)
        {
            CoordinateCalculatorFactory factory = new CoordinateCalculatorFactory();
            fallbackCalculator = factory.New(axisParams);
        }
        public double GetCoordinate(DateTime dataValue)
        {
            return fallbackCalculator.GetCoordinate(dataValue);
        }

        public double GetCoordinate(double dataValue)
        {
            double temp = mod(dataValue, 10000);
            return fallbackCalculator.GetCoordinate(temp);
        }

        public void GetCoordinates(double[] dataValues, double[] coordinates, double offset = 0)
        {
            fallbackCalculator.GetCoordinates(dataValues, coordinates, offset);
        }

        public void GetCoordinates(double[] dataValues, double[] coordinates, int count, double offset = 0)
        {
            fallbackCalculator.GetCoordinates(dataValues, coordinates, count, offset);
        }

        public double GetDataValue(double pixelCoordinate)
        {
            return mod(fallbackCalculator.GetDataValue(pixelCoordinate), 10000);
        }

        public CoordinateCalculator ToNativeCalculator(eAxisType axisType, Type dataType)
        {
            throw new NotImplementedException();
        }

        public DoubleRange TranslateBy(double pixels, DoubleRange inputRange)
        {
            return fallbackCalculator.TranslateBy(pixels, inputRange);
        }

        public DoubleRange TranslateBy(double minFraction, double maxFraction, IRange inputRange)
        {
            return fallbackCalculator.TranslateBy(minFraction, maxFraction, inputRange);
        }
    }

    public class CustomHeatmapRenderableSeries : FastUniformHeatmapRenderableSeries, IRenderableSeriesBase
    {
        public CustomHeatmapRenderableSeries()
            : base()
        { 
        }

        protected override void InternalDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            base.InternalDraw(renderContext, renderPassData);
        }
    }


    /// <summary>
    /// Custom wraparound axis
    /// </summary>
    public class WraparoundNumericAxis : NumericAxis
    {
        private IRange actualRange = new DoubleRange(0, 10000);

        public WraparoundNumericAxis()
            : base()
        {
        }

        double mod(double x, double m)
        {
            return (x % m + m) % m;
        }

        //public override IComparable GetDataValue(double pixelCoordinate)
        //{
        //    IComparable baseDataValue = base.GetDataValue(pixelCoordinate);
        //    if (baseDataValue is double doubleValue)
        //    {
        //        return mod(doubleValue, actualRange.AsDoubleRange().Max);
        //    }
        //    else return baseDataValue;
        //}

        public override double GetCoordinate(IComparable value)
        {
            if (value is double doubleValue)
            {
                return base.GetCoordinate(mod(doubleValue, actualRange.AsDoubleRange().Max));
            }
            return base.GetCoordinate(value);
        }

        public override ICoordinateCalculator<double> GetCurrentCoordinateCalculator()
        {
            ICoordinateCalculator<double> temp = new WraparoundCoordinateCalculator(GetAxisParams());
            return temp;
        }
    }

    /// <summary>
    /// Interaction logic for ChartView.xaml
    /// </summary>
    public partial class ChartView
    {
        private readonly UniformHeatmapDataSeries<double, double, double>[] _graphData;
        private UniformHeatmapDataSeries<double, double, double> GraphData => _graphData[DisplayIndex];
        private int DisplayIndex { get; set; }

        private static UniformHeatmapDataSeries<double, double, double> GenerateRandomData()
        {
            var xDimension = 10000;
            var yDimension = 10000;
            
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.WriteLine($"{timestamp} Generating random data...");
            var randomGenerator = new Random();
            var zValues = new double[xDimension];
            
            for (var i = 0; i < xDimension; i++)
            {
                zValues[i] = (double)randomGenerator.NextDouble() * 100;
            }
            
            var data = new double[xDimension, yDimension];
            
            for (var x = 0; x < xDimension; x++)
            {
                for (var y = 0; y < yDimension; y++)
                {
                    data[x, y] = zValues[x];
                }
            }
            
            return new UniformHeatmapDataSeries<double, double, double>(data, 0, 1, 0, 1);
        }

        private void UpdateDisplayedFrameNumber()
        {
            FrameRun.Text = $"Displaying frame: {DisplayIndex}";
        }
        
        public ChartView()
        {
            InitializeComponent();
            
            _graphData = new UniformHeatmapDataSeries<double, double, double>[2];
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
