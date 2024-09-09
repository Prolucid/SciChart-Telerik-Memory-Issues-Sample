using System.Windows;
using SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Charting.Visuals.PaletteProviders;
using SciChart.Charting.Visuals.RenderableSeries.DrawingProviders;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.Axes.AxisInteractivityHelpers;
using SciChart.Drawing.Common;
using SciChart.Charting.Numerics.CoordinateCalculators;
using SciChart.Data.Model;
using SciChart.Charting2D.Interop;
using System.Windows.Media;
using SciChart.Core.Extensions;

namespace Views
{
    public class CustomPaletteProvider : IHeatmapPaletteProvider
    {
        public void OnBeginSeriesDraw(IRenderableSeries rSeries)
        {
        }

        public Color? OverrideCellColor(IRenderableSeries rSeries, int xIndex, int yIndex, IComparable zValue, Color cellColor, IPointMetadata metadata)
        {
            Color c = Color.FromRgb(255, 0, 0);
            return c;
        }
    }
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

        public int GetCellCountInRange(IRange absoluteXRange) // needs to take a range not a diff
        {
            DoubleRange doubleAbsoluteRange = absoluteXRange.AsDoubleRange();
            // Go to first location with a point
            int minRangeIndex = (int)Math.Floor((doubleAbsoluteRange.Min - _xStart) / _xStep);
            int maxRangeIndex = (int)Math.Ceiling((doubleAbsoluteRange.Max - _xStart) / _xStep);
            int cellCount = maxRangeIndex - minRangeIndex;

            // start counting by the step value until we reach the max
            //while (((minRangeIndex + count) * _xStep) < doubleAbsoluteRange.Max) // sloppy while loop for now, there is a formula to solve this
            //{ 
            //    count++;
            //}
            return cellCount;
        }

        // function to get minimum instance of a mod value in given range
        public (double, double) GetMinIndexInstanceValue(IRange searchRange, int modXIndex)
        {
            DoubleRange doubleSearchRange = searchRange.AsDoubleRange();
            // floor/ceil search range to next values that align with xIndices
            int minSearchRangeIndex = (int)Math.Floor((doubleSearchRange.Min - _xStart) / _xStep);
            int maxSearchRangeIndex = (int)Math.Ceiling((doubleSearchRange.Max - _xStart) / _xStep);
            // formula to find the smallest x such that (x mod _xSize = modXIndex)
            int minIndex = minSearchRangeIndex + (int)mod(modXIndex - minSearchRangeIndex, _xSize);
            return (GetXValue(minIndex), GetXValue(maxSearchRangeIndex));
        }

    }

    public class WraparoundHeatmapDrawingProvider : UniformHeatmapSeriesDrawingProvider
    {
        public WraparoundHeatmapDrawingProvider(FastUniformHeatmapRenderableSeries renderableSeries) : base(renderableSeries)
        { 
        }
        private void GetColorDataNoPeakDetector(int xStartInd, int width, int yStartind, int height, int yInc, double opacity, HeatmapColorPalette colorMap, IHeatmapPaletteProvider pp, CustomHeatmapDS dataseries)
        {
            // todo: parallelize
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

        private int DecimatedTextureSize(int dataCells, double heatmapRectSize, out int increment)
        {
            increment = 1;
            int decimatedSize = dataCells;
            if (dataCells > heatmapRectSize)
            {
                increment = (int) (dataCells / heatmapRectSize);
                decimatedSize /= increment;
            }
            return decimatedSize;
        }

        protected override void GetColorDataForTexture(int xStartInd, int textureWidth, int xInc, int yStartInd, int textureHeight, int yInc)
        {

            // Really this is just responsible for setting _colorData. The mapping from raw data -> color happens here

            // basically just call either the peak detector or non-peak detector based on this.PeakDetector. (Just call base if PeakDetector is on)
            // implement non-peak detector here aswell, essentially just distributes yslices to be handled in parallel if multithreading is enabled

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
            //base.GetColorDataForTexture(xStartInd, textureWidth, xInc, yStartInd, textureHeight, yInc);
        }

        protected override void DrawHeatmapAsTexture(IRenderContext2D renderContext, Rect heatmapRect)
        {
            
            //Size viewportSize = renderContext.ViewportSize;
            
            //int width = (int) renderContext.ViewportSize.Width.RoundOff(MidpointRounding.AwayFromZero);
            //double ytop = heatmapRect.Top < 0.0 ? 0.0 : heatmapRect.Top;
            //double ybot = heatmapRect.Bottom > viewportSize.Height ? viewportSize.Height : heatmapRect.Bottom;
            //int height = (int) (ybot - ytop).RoundOff(MidpointRounding.AwayFromZero);
            //this.DrawHeatmapAsTexture(renderContext, overwriteRect, width, height, true);

            // need to overwrite _horStartInd and _horCellCount
            // _horStartInd should be the visibleRange.Min % xRange
            // _horCellCount should be index range covered by the visible range

            
            IComparable visibleMin = this.RenderableSeries.XAxis.VisibleRange.Min;
            IComparable visibleDiff = this.RenderableSeries.XAxis.VisibleRange.Diff;
            CustomHeatmapDS ds = (CustomHeatmapDS)this.RenderableSeries.DataSeries;
            this._horStartInd = ds.getWrappedXIndex(visibleMin);
            this._horCellCount = ds.GetCellCountInRange(this.RenderableSeries.XAxis.VisibleRange);
            //double horStartVal = ds.GetXValue(this._horStartInd);

            (double minValue, double maxValue) = ds.GetMinIndexInstanceValue(this.RenderableSeries.XAxis.VisibleRange, this._horStartInd); // Does this need the mod part?
            double minCoord = this.RenderableSeries.XAxis.GetCurrentCoordinateCalculator().GetCoordinate(minValue);
            double maxCoord = this.RenderableSeries.XAxis.GetCurrentCoordinateCalculator().GetCoordinate(maxValue);

            double heatmapWidth = maxCoord - minCoord;

            // width is just the diff in coordinates between minValue and maxValue

            // We need to overwrite the starting position and size of the heatmapRect because scichart doesn't like non-sequential ranges
            Rect overwriteRect = new Rect(minCoord, heatmapRect.Top, heatmapWidth, heatmapRect.Height);

            //int xInc, yInc;
            //int textureWidth = DecimatedTextureSize(this._horCellCount, overwriteRect.Width, out xInc);
            //int textureHeight = DecimatedTextureSize(this._vertCellCount, overwriteRect.Height, out yInc);
            //this._horInc *= xInc;
            //this._vertInc *= yInc;

            //base.DrawHeatmapAsTexture(renderContext, overwriteRect, textureWidth, textureHeight, true);
            base.DrawHeatmapAsTexture(renderContext, overwriteRect);

        }

        protected override void DrawHeatmapAsTexture(IRenderContext2D renderContext, Rect destinationRect, int textureWidth, int textureHeight, bool isUniform)
        {
            base.DrawHeatmapAsTexture(renderContext, destinationRect, textureWidth, textureHeight, isUniform);
        }

        //public override void OnDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        //{
        //    CustomHeatmapDS dataSeries = (CustomHeatmapDS)this.RenderableSeries.DataSeries;

        //    IRange visibleRangeY = this.RenderableSeries.YAxis.VisibleRange;
        //    IndexRange yIndicesRange = dataSeries.GetYIndicesRange(visibleRangeY);
        //    double ydataValue1 = (double)dataSeries.GetYValue(yIndicesRange.Min);
        //    double ydataValue2 = (double)dataSeries.GetYValue(yIndicesRange.Max);
        //    double ycoord1 = renderPassData.YCoordinateCalculator.GetCoordinate(ydataValue1);
        //    double ycoord2 = renderPassData.YCoordinateCalculator.GetCoordinate(ydataValue2);
        //    double height = ycoord2 - ycoord1;


        //    double xdataValue1 = 0;
        //    double xdataValue2 = 9999;
        //    double xcoord1 = renderPassData.XCoordinateCalculator.GetCoordinate(xdataValue1);
        //    double xcoord2 = renderPassData.XCoordinateCalculator.GetCoordinate(xdataValue2);
        //    double width = xcoord2 - xcoord1;

        //    Rect heatmapRect = new Rect(ycoord1, xcoord1, Math.Abs(width), Math.Abs(height));

        //    this._horStartInd = yIndicesRange.Min;
        //    this._vertStartInd = 0;
        //    this._horInc = -1;
        //    this._vertInc = 1;
        //    this._horCellCount = yIndicesRange.Diff;
        //    this._vertCellCount = 9999;
        //    this._zValues = dataSeries.getZValues();

        //    this.DrawHeatmapAsRects(renderContext, heatmapRect);
        //    //else {
        //    //    this.DrawHeatmapAsTexture(renderContext, heatmapRect);    
        //    //}
        //    this.DrawHeatmapLabels(renderContext, heatmapRect);
        //    //base.OnDraw(renderContext, renderPassData);
        //}

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

        public IRange ActiveVisibleRange;

        private ICoordinateCalculator<double> fallbackCalculator;

        public WraparoundCoordinateCalculator(AxisParams axisParams, IRange activeVisibleRange)
        {
            ActiveVisibleRange = activeVisibleRange;
            CoordinateCalculatorFactory factory = new CoordinateCalculatorFactory();
            fallbackCalculator = factory.New(axisParams);
        }
        public double GetCoordinate(DateTime dataValue)
        {
            return fallbackCalculator.GetCoordinate(dataValue);
        }

        public double GetCoordinate(double dataValue)
        {
            // HeatmapDrawingProvider is doing a check to make sure x-min < x-max which is why the heatmap only renders under certain visible range conditions
            // probably need a custom DrawingProvider
            //double temp = mod(dataValue, 10000);
            //double temp2 = fallbackCalculator.GetCoordinate(temp);
            //return temp2;
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
            return mod(fallbackCalculator.GetDataValue(pixelCoordinate), 10);
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

    // Custom RenderableSeries that inherits from FastUniformHeatmapRenderableSeries and overrides InternalDraw
    public class CustomHeatmapRenderableSeries : FastUniformHeatmapRenderableSeries, IRenderableSeriesBase
    {
        public CustomHeatmapRenderableSeries()
            : base()
        {
            this.DrawingProviders = (IEnumerable<ISeriesDrawingProvider>)new WraparoundHeatmapDrawingProvider[1]
              {
                new WraparoundHeatmapDrawingProvider(this)
                {
                  PeakDetector = (IHeatmapPeakDetector) new UniformHeatmapYPeakDetector()
                }
              };
            Opacity = 1;
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
        private IRange actualRange = new DoubleRange(0, 10);

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
        
        // The VisibleRange being requested by the drawing provider needs to be overwritten to provide the actual visible range
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
            // Figure out what drawing providers are doing during OnInvalidateParentSurface() method of RenderableSeries

            ICoordinateCalculator<double> coordCalc = new WraparoundCoordinateCalculator(GetAxisParams(), this.VisibleRange);
            // interactivity helper has its own ref to coordinate calculator
            this._currentInteractivityHelper = AxisInteractivityHelperFactory.New(this.GetAxisParams(), coordCalc);
            return coordCalc;
        }

        protected override IComparable ConvertTickToDataValue(IComparable value)
        {
            if (value is double doubleValue)
                return mod(doubleValue, actualRange.AsDoubleRange().Max);
            else return value;
        }

        public override IComparable GetDataValue(double pixelCoordinate)
        {
            ICoordinateCalculator<double> coordinateCalculator = GetCurrentCoordinateCalculator();
            IComparable temp = coordinateCalculator.GetDataValue(pixelCoordinate);
            return temp;
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
