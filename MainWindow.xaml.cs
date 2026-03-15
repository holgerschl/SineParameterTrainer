using System.Windows;
using System.Windows.Input;
using SineParameterTrainer.Models;
using SineParameterTrainer.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;

namespace SineParameterTrainer;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    private VerticalLine? _vLine1, _vLine2;
    private HorizontalLine? _hLine1, _hLine2;
    private Text? _label1, _label2;

    private enum DragTarget { None, VLine1, VLine2, HLine1, HLine2 }
    private DragTarget _currentDrag = DragTarget.None;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        viewModel.OnNewProblem = UpdatePlot;

        WpfPlot.MouseLeftButtonDown += WpfPlot_MouseLeftButtonDown;
        WpfPlot.MouseMove += WpfPlot_MouseMove;
        WpfPlot.MouseLeftButtonUp += WpfPlot_MouseLeftButtonUp;

        Loaded += (_, _) => viewModel.GenerateNewCommand.Execute(null);
    }

    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            _viewModel.CheckAnswerCommand.Execute(null);
    }

    private Coordinates GetMouseCoordinates(MouseEventArgs e)
    {
        var pos = e.GetPosition(WpfPlot);
        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(WpfPlot);
        var pixel = new Pixel((float)(pos.X * dpi.DpiScaleX), (float)(pos.Y * dpi.DpiScaleY));
        return WpfPlot.Plot.GetCoordinates(pixel);
    }

    private void WpfPlot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_vLine1 is null || _vLine2 is null || _hLine1 is null || _hLine2 is null)
            return;

        var coord = GetMouseCoordinates(e);

        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(WpfPlot);
        var pxA = new Pixel(0, 0);
        var pxB = new Pixel((float)(15 * dpi.DpiScaleX), (float)(15 * dpi.DpiScaleY));
        var cA = WpfPlot.Plot.GetCoordinates(pxA);
        var cB = WpfPlot.Plot.GetCoordinates(pxB);
        double xThresh = Math.Abs(cB.X - cA.X);
        double yThresh = Math.Abs(cB.Y - cA.Y);

        if (Math.Abs(coord.X - _vLine1.X) < xThresh)
            _currentDrag = DragTarget.VLine1;
        else if (Math.Abs(coord.X - _vLine2!.X) < xThresh)
            _currentDrag = DragTarget.VLine2;
        else if (Math.Abs(coord.Y - _hLine1.Y) < yThresh)
            _currentDrag = DragTarget.HLine1;
        else if (Math.Abs(coord.Y - _hLine2!.Y) < yThresh)
            _currentDrag = DragTarget.HLine2;
        else
            return;

        WpfPlot.UserInputProcessor.IsEnabled = false;
        WpfPlot.CaptureMouse();
        e.Handled = true;
    }

    private void WpfPlot_MouseMove(object sender, MouseEventArgs e)
    {
        if (_currentDrag == DragTarget.None)
            return;

        var coord = GetMouseCoordinates(e);

        switch (_currentDrag)
        {
            case DragTarget.VLine1: _vLine1!.X = coord.X; break;
            case DragTarget.VLine2: _vLine2!.X = coord.X; break;
            case DragTarget.HLine1: _hLine1!.Y = coord.Y; break;
            case DragTarget.HLine2: _hLine2!.Y = coord.Y; break;
        }

        UpdateCrosshairLabels();
        WpfPlot.Refresh();
        UpdateDistanceDisplay();
    }

    private void WpfPlot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_currentDrag != DragTarget.None)
        {
            _currentDrag = DragTarget.None;
            WpfPlot.ReleaseMouseCapture();
            WpfPlot.UserInputProcessor.IsEnabled = true;
        }
    }

    private void UpdateCrosshairLabels()
    {
        if (_vLine1 is null || _hLine1 is null || _vLine2 is null || _hLine2 is null)
            return;

        if (_label1 is not null)
        {
            _label1.Location = new Coordinates(_vLine1.X, _hLine1.Y);
            _label1.LabelText = $"({_vLine1.X:F2}, {_hLine1.Y:F2})";
        }

        if (_label2 is not null)
        {
            _label2.Location = new Coordinates(_vLine2.X, _hLine2.Y);
            _label2.LabelText = $"({_vLine2.X:F2}, {_hLine2.Y:F2})";
        }
    }

    private void UpdateDistanceDisplay()
    {
        if (_vLine1 is null || _vLine2 is null || _hLine1 is null || _hLine2 is null)
            return;

        double dx = Math.Abs(_vLine2.X - _vLine1.X);
        double dy = Math.Abs(_hLine2.Y - _hLine1.Y);
        _viewModel.DeltaX = $"\u0394x = {dx:F2}";
        _viewModel.DeltaY = $"\u0394y = {dy:F2}";
    }

    private void UpdatePlot(SineParameters p)
    {
        var plt = WpfPlot.Plot;
        plt.Clear();

        double period = 2 * Math.PI / Math.Abs(p.B);

        double tMin = Math.Min(-1, p.C - period) - 0.5;
        double tMax = Math.Max(1, p.C + 2.5 * period) + 0.5;

        const int points = 1000;
        double[] t = new double[points];
        double[] y = new double[points];
        double step = (tMax - tMin) / (points - 1);

        for (int i = 0; i < points; i++)
        {
            t[i] = tMin + i * step;
            y[i] = p.A * Math.Sin(p.B * (t[i] - p.C)) + p.D;
        }

        // Sine curve
        var scatter = plt.Add.Scatter(t, y);
        scatter.LineWidth = 3;
        scatter.MarkerSize = 0;
        scatter.Color = Colors.Blue;

        // Subtle axis zero-lines
        var hZero = plt.Add.HorizontalLine(0);
        hZero.LineWidth = 1;
        hZero.Color = Colors.Black.WithAlpha(.25);

        var vZero = plt.Add.VerticalLine(0);
        vZero.LineWidth = 1;
        vZero.Color = Colors.Black.WithAlpha(.25);

        // Draggable help-line crosshair 1 (red)
        double xMid = (tMin + tMax) / 2;
        double x1 = xMid - period * 0.4;
        double y1 = p.D + Math.Abs(p.A) * 0.5;

        _vLine1 = plt.Add.VerticalLine(x1);
        _vLine1.Color = Colors.Red.WithAlpha(.6);
        _vLine1.LineWidth = 2;
        _vLine1.LinePattern = LinePattern.Dashed;

        _hLine1 = plt.Add.HorizontalLine(y1);
        _hLine1.Color = Colors.Red.WithAlpha(.6);
        _hLine1.LineWidth = 2;
        _hLine1.LinePattern = LinePattern.Dashed;

        // Draggable help-line crosshair 2 (green)
        double x2 = xMid + period * 0.4;
        double y2 = p.D - Math.Abs(p.A) * 0.5;

        _vLine2 = plt.Add.VerticalLine(x2);
        _vLine2.Color = Colors.Green.WithAlpha(.7);
        _vLine2.LineWidth = 2;
        _vLine2.LinePattern = LinePattern.Dashed;

        _hLine2 = plt.Add.HorizontalLine(y2);
        _hLine2.Color = Colors.Green.WithAlpha(.7);
        _hLine2.LineWidth = 2;
        _hLine2.LinePattern = LinePattern.Dashed;

        // Coordinate labels at crosshair intersections
        _label1 = plt.Add.Text($"({x1:F2}, {y1:F2})", x1, y1);
        _label1.LabelFontSize = 28;
        _label1.LabelFontColor = Colors.Red;
        _label1.LabelBackgroundColor = Colors.White.WithAlpha(.85);
        _label1.LabelBorderColor = Colors.Red.WithAlpha(.4);
        _label1.LabelBorderWidth = 1;
        _label1.OffsetX = 10;
        _label1.OffsetY = 10;

        _label2 = plt.Add.Text($"({x2:F2}, {y2:F2})", x2, y2);
        _label2.LabelFontSize = 28;
        _label2.LabelFontColor = Colors.Green;
        _label2.LabelBackgroundColor = Colors.White.WithAlpha(.85);
        _label2.LabelBorderColor = Colors.Green.WithAlpha(.4);
        _label2.LabelBorderWidth = 1;
        _label2.OffsetX = 10;
        _label2.OffsetY = 10;

        // Axis limits with padding
        double yMin = p.D - Math.Abs(p.A) - 2;
        double yMax = p.D + Math.Abs(p.A) + 2;
        plt.Axes.SetLimits(tMin, tMax, yMin, yMax);

        // Style
        plt.Title("");
        plt.XLabel("t");
        plt.YLabel("f(t)");
        plt.Grid.MajorLineColor = Color.FromHex("#E2E8F0");

        plt.Axes.Title.Label.FontSize = 36;
        plt.Axes.Bottom.Label.FontSize = 32;
        plt.Axes.Left.Label.FontSize = 32;
        plt.Axes.Bottom.TickLabelStyle.FontSize = 28;
        plt.Axes.Left.TickLabelStyle.FontSize = 28;

        WpfPlot.Refresh();
        UpdateDistanceDisplay();
    }
}
