using System.Windows;
using SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Charting.Visuals.PaletteProviders;
using SciChart.Charting.Visuals.RenderableSeries.DrawingProviders;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.Axes.AxisInteractivityHelpers;
using SciChart.Charting.Visuals;
using SciChart.Drawing.Common;
using SciChart.Charting.Numerics.CoordinateCalculators;
using SciChart.Data.Model;
using SciChart.Charting2D.Interop;
using System.Windows.Media;
using SciChart.Core.Extensions;

namespace Views
{
    public class CustomHeatmapDS : UniformHeatmapDataSeries<double, double, double>, IHeatmapDataSeries
    {
        double mod(double x, double m)
        {
            return (x % m + m) % m;
        }

        private double _xStep;
        private double _xStart;
        private int _xSize;
        public CustomHeatmapDS(double[,] zValues, double xStart, double xStep, double yStart, double yStep)
            : base(zValues, xStart, xStep, yStart, yStep)
        {
            _xStep = xStep;
            _xStart = xStart;
            _xSize = zValues.GetLength(1);
        }

        public int getWrappedXIndex(IComparable absoluteXValue)
        {
            // not really sure if its a good idea to be doing modulo algebra on non-index values
            DoubleRange xRange = GetXRange().AsDoubleRange();
            double doubleAbsolute = absoluteXValue.ToDouble();
            double xMax = _xStart + _xStep * _xSize;
            double wrappedAbsolute = mod(doubleAbsolute, xMax) + _xStart;
            int temp = GetXIndex(wrappedAbsolute);
            return temp;
        }

        public int GetCellCountInRange(IRange absoluteXRange)
        {
            IntegerRange indexRange = GetIndexRange(absoluteXRange);
            return indexRange.Diff;
        }

        // Get the Indices of the first and last data points to display in a given range (indices can be negative)
        public IntegerRange GetIndexRange(IRange absoluteRange)
        {
            DoubleRange doubleAbsoluteRange = absoluteRange.AsDoubleRange();
            int minRangeIndex = (int)Math.Floor((doubleAbsoluteRange.Min - _xStart) / _xStep);
            int maxRangeIndex = (int)Math.Ceiling((doubleAbsoluteRange.Max - _xStart) / _xStep);
            return new IntegerRange(minRangeIndex, maxRangeIndex);
        }

        // Get the X location of the first and last data points to display in a given range
        public DoubleRange GetDataPointLocationsInRange(IRange searchRange)
        {
            IntegerRange indexRange = GetIndexRange(searchRange);
            return new DoubleRange(GetXValue(indexRange.Min), GetXValue(indexRange.Max)); // translate indices to data point locations
        }

        public double GetXMax()
        {
            return _xStart + (_xStep * _xSize);
        }
    }

    public class WraparoundHeatmapDrawingProvider : UniformHeatmapSeriesDrawingProvider
    {
        // HeatmapDrawingProvider is doing a check to make sure x-min < x-max which is why the heatmap only renders under certain visible range conditions
        public WraparoundHeatmapDrawingProvider(FastUniformHeatmapRenderableSeries renderableSeries) : base(renderableSeries)
        { 
        }
        private void GetColorDataNoPeakDetector(int xStartInd, int width, int yStartind, int height, int yInc, double opacity, HeatmapColorPalette colorMap, IHeatmapPaletteProvider pp, CustomHeatmapDS dataseries)
        {
            // todo: parallelize, which just distributes y slices to be handled in parallel
            bool isMultithreaded = this.RenderableSeries.GetParentSurface().EnableMultiThreadedRendering;
            if (isMultithreaded)
            {
                for (int y = 0; y < height; y++)
                {
                    int yInd = yStartind + (y * yInc);

                    for (int x = 0; x < width; x++)
                    {
                        int xInd = dataseries.getWrappedXIndex(xStartInd + x);
                        int colorIndex = (y * width) + x;
                        this._colorData[colorIndex] = this.GetColor(dataseries, yInd, xInd, colorMap, pp, opacity);
                    }
                }
            }
        }

        protected override void GetColorDataForTexture(int xStartInd, int textureWidth, int xInc, int yStartInd, int textureHeight, int yInc)
        {

            // This method is really just responsible for setting _colorData. The mapping from raw data -> color happens here
            
            // todo: look into handling with a peak detector
            this.GetColorDataNoPeakDetector(
                xStartInd, 
                textureWidth, 
                yStartInd, 
                textureHeight, 
                yInc, 
                this.RenderableSeries.Opacity, 
                this.RenderableSeries.ColorMap, 
                this.RenderableSeries.PaletteProvider as IHeatmapPaletteProvider, 
                this.RenderableSeries.DataSeries as CustomHeatmapDS);
        }

        protected override void DrawHeatmapAsTexture(IRenderContext2D renderContext, Rect heatmapRect)
        {            
            IComparable visibleMin = this.RenderableSeries.XAxis.VisibleRange.Min;
            CustomHeatmapDS ds = (CustomHeatmapDS)this.RenderableSeries.DataSeries;
            this._horStartInd = ds.getWrappedXIndex(visibleMin);
            this._horCellCount = ds.GetCellCountInRange(this.RenderableSeries.XAxis.VisibleRange);
            
            // Calculations for sizing/positioning the X-Axis of the texture rect
            DoubleRange dataPointRange = ds.GetDataPointLocationsInRange(this.RenderableSeries.XAxis.VisibleRange);
            // minCoord and maxCoord can overflow outside the VisibleRange
            double minCoord = this.RenderableSeries.XAxis.GetCoordinate(dataPointRange.Min);
            double maxCoord = this.RenderableSeries.XAxis.GetCoordinate(dataPointRange.Max);
            // Texture rect width is just the diff between screen coordinates for Min and Max visible x values
            double heatmapWidth = maxCoord - minCoord;

            // We need to overwrite the starting position and size of the heatmapRect because scichart doesn't like non-sequential ranges
            Rect overwriteRect = new Rect(minCoord, heatmapRect.Top, heatmapWidth, heatmapRect.Height);

            base.DrawHeatmapAsTexture(renderContext, overwriteRect);

        }
    }

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
        private double WrapValue;

        public WraparoundCoordinateCalculator(AxisParams axisParams, double wrapValue)
        {
            CoordinateCalculatorFactory factory = new CoordinateCalculatorFactory();
            fallbackCalculator = factory.New(axisParams);
            WrapValue = wrapValue;
        }
        public double GetCoordinate(DateTime dataValue)
        {
            return fallbackCalculator.GetCoordinate(dataValue);
        }

        public double GetCoordinate(double dataValue)
        {
            return fallbackCalculator.GetCoordinate(dataValue);
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
            return mod(fallbackCalculator.GetDataValue(pixelCoordinate), WrapValue);
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
            DrawingProviders = (IEnumerable<ISeriesDrawingProvider>)new WraparoundHeatmapDrawingProvider[1]
              {
                new WraparoundHeatmapDrawingProvider(this)
              };
        }
    }


    /// <summary>
    /// Custom wraparound axis
    /// </summary>
    public class WraparoundNumericAxis : NumericAxis
    {
        public WraparoundNumericAxis()
            : base()
        {
        }

        double mod(double x, double m)
        {
            return (x % m + m) % m;
        }

        public override ICoordinateCalculator<double> GetCurrentCoordinateCalculator()
        {
            if (this.ParentSurface is SciChartSurface parentSurface)
            {
                if (parentSurface.RenderableSeries[0].DataSeries is CustomHeatmapDS heatmapDS)
                {
                    ICoordinateCalculator<double> coordCalc = new WraparoundCoordinateCalculator(GetAxisParams(), heatmapDS.GetXMax());
                    // interactivity helper has its own ref to coordinate calculator
                    this._currentInteractivityHelper = AxisInteractivityHelperFactory.New(this.GetAxisParams(), coordCalc);
                    return coordCalc;
                }
            }
            return base.GetCurrentCoordinateCalculator();
        }

        protected override IComparable ConvertTickToDataValue(IComparable value)
        {
            if (value is double doubleValue && this.ParentSurface is SciChartSurface parentSurface)
            {
                if (parentSurface.RenderableSeries[0].DataSeries is CustomHeatmapDS heatmapDS)
                {
                    return mod(doubleValue, heatmapDS.GetXMax());
                }
            }
            return value;
        }

        public override IComparable GetDataValue(double pixelCoordinate)
        {
            ICoordinateCalculator<double> coordinateCalculator = GetCurrentCoordinateCalculator();
            return coordinateCalculator.GetDataValue(pixelCoordinate);
        }
    }

    /// <summary>
    /// Interaction logic for ChartView.xaml
    /// </summary>
    public partial class ChartView
    {
        private readonly CustomHeatmapDS[] _graphData;
        private CustomHeatmapDS GraphData => _graphData[DisplayIndex];
        private int DisplayIndex { get; set; }

        private static CustomHeatmapDS GenerateRandomData()
        {
            var xDimension = 5;
            var yDimension = 10;
            
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.WriteLine($"{timestamp} Generating random data...");
            var randomGenerator = new Random();
            var zValues = new double[yDimension];
            
            for (var i = 0; i < yDimension; i++)
            {
                zValues[i] = (double)randomGenerator.NextDouble() * 100;
            }
            
            var data = new double[xDimension, yDimension];
            
            for (var x = 0; x < xDimension; x++)
            {
                for (var y = 0; y < yDimension; y++)
                {
                    data[x, y] = zValues[y];
                }
            }
            
            return new CustomHeatmapDS(data, 0, 1, 0, 1);
        }

        private void UpdateDisplayedFrameNumber()
        {
            FrameRun.Text = $"Displaying frame: {DisplayIndex}";
        }
        
        public ChartView()
        {
            InitializeComponent();
            
            _graphData = new CustomHeatmapDS[2];
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
