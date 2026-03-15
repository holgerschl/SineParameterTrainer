using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SineParameterTrainer.Models;
using SineParameterTrainer.Services;

namespace SineParameterTrainer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISineCurveService _curveService;

    [ObservableProperty] private string _userA = "";
    [ObservableProperty] private string _userB = "";
    [ObservableProperty] private string _userC = "";
    [ObservableProperty] private string _userD = "";
    [ObservableProperty] private string _feedback = "";
    [ObservableProperty] private string _feedbackColor = "#1E293B";
    [ObservableProperty] private bool _solutionVisible;
    [ObservableProperty] private string _solutionText = "";
    [ObservableProperty] private int _score;
    [ObservableProperty] private int _totalAttempts;
    [ObservableProperty] private string _deltaX = "";
    [ObservableProperty] private string _deltaY = "";
    [ObservableProperty] private bool _formulaVisible = true;

    public SineParameters? CurrentParameters { get; private set; }

    public Action<SineParameters>? OnNewProblem { get; set; }

    public MainViewModel(ISineCurveService curveService)
    {
        _curveService = curveService;
    }

    [RelayCommand]
    private void GenerateNew()
    {
        CurrentParameters = _curveService.GenerateRandomParameters();
        UserA = "";
        UserB = "";
        UserC = "";
        UserD = "";
        Feedback = "";
        SolutionVisible = false;
        SolutionText = "";
        OnNewProblem?.Invoke(CurrentParameters);
    }

    [RelayCommand]
    private void CheckAnswer()
    {
        if (CurrentParameters is null) return;

        if (!TryParseInput(UserA, out double a) ||
            !TryParseInput(UserB, out double b) ||
            !TryParseInput(UserC, out double c) ||
            !TryParseInput(UserD, out double d))
        {
            Feedback = "Please enter valid numbers for all parameters (e.g. 3, -1.5, or 3/2).";
            FeedbackColor = "#EF4444";
            return;
        }

        TotalAttempts++;

        // Robust check: evaluate both functions at many points to handle equivalent representations
        if (FunctionsMatch(a, b, c, d, CurrentParameters))
        {
            Score++;
            Feedback = $"Correct! Well done!  ({Score} / {TotalAttempts})";
            FeedbackColor = "#10B981";
            return;
        }

        // Give specific feedback on which parameters look wrong (direct comparison)
        var wrong = new List<string>();
        if (Math.Abs(a - CurrentParameters.A) > 0.05) wrong.Add("a");
        if (Math.Abs(b - CurrentParameters.B) > 0.05) wrong.Add("b");
        if (Math.Abs(c - CurrentParameters.C) > 0.05) wrong.Add("c");
        if (Math.Abs(d - CurrentParameters.D) > 0.05) wrong.Add("d");

        if (wrong.Count == 0)
            wrong.Add("values (try a different equivalent form)");

        Feedback = $"Not quite. Check: {string.Join(", ", wrong)}  ({Score} / {TotalAttempts})";
        FeedbackColor = "#EF4444";
    }

    [RelayCommand]
    private void RevealSolution()
    {
        if (CurrentParameters is null) return;
        SolutionVisible = true;
        var p = CurrentParameters;
        SolutionText = $"a = {FormatParam(p.A)},   b = {FormatParam(p.B)},   c = {FormatParam(p.C)},   d = {FormatParam(p.D)}";
    }

    private static bool FunctionsMatch(double a, double b, double c, double d, SineParameters actual)
    {
        for (int i = 0; i < 200; i++)
        {
            double t = -10.0 + i * 0.1;
            double y1 = a * Math.Sin(b * (t - c)) + d;
            double y2 = actual.A * Math.Sin(actual.B * (t - actual.C)) + actual.D;
            if (Math.Abs(y1 - y2) > 0.05) return false;
        }
        return true;
    }

    private static string FormatParam(double value)
    {
        if (Math.Abs(value - Math.Round(value)) < 0.001)
            return ((int)Math.Round(value)).ToString();
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static bool TryParseInput(string input, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;

        input = input.Trim().Replace(',', '.');

        if (input.Contains('/'))
        {
            var parts = input.Split('/');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], CultureInfo.InvariantCulture, out double num) &&
                double.TryParse(parts[1], CultureInfo.InvariantCulture, out double den) &&
                Math.Abs(den) > 0.0001)
            {
                value = num / den;
                return true;
            }
            return false;
        }

        return double.TryParse(input, CultureInfo.InvariantCulture, out value);
    }
}
