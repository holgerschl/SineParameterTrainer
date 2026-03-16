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
    [ObservableProperty] private bool _isCheckEnabled = true;

    private string _lang = "de";

    private static readonly Dictionary<string, Dictionary<string, string>> I18n = new()
    {
        ["title"] = new() { ["de"] = "Modellierung der allgemeinen Sinusfunktion", ["en"] = "Sine Parameter Trainer" },
        ["subtitle"] = new() { ["de"] = "Bestimme die Parameter a, b, c, d aus dem Graphen", ["en"] = "Determine the parameters a, b, c, d from the graph" },
        ["showFormula"] = new() { ["de"] = "Formel anzeigen", ["en"] = "Show formula" },
        ["btnCheck"] = new() { ["de"] = "Antwort pr\u00fcfen", ["en"] = "Check Answer" },
        ["btnNew"] = new() { ["de"] = "Neue Aufgabe", ["en"] = "New Problem" },
        ["btnSolution"] = new() { ["de"] = "L\u00f6sung anzeigen", ["en"] = "Show Solution" },
        ["measurement"] = new() { ["de"] = "Messung:", ["en"] = "Measurement:" },
        ["dragHint"] = new() { ["de"] = "(Hilfslinien im Graphen ziehen)", ["en"] = "(drag the dashed lines on the plot)" },
        ["score"] = new() { ["de"] = "Punkte: ", ["en"] = "Score: " },
        ["correct"] = new() { ["de"] = "Richtig! Gut gemacht!", ["en"] = "Correct! Well done!" },
        ["notQuite"] = new() { ["de"] = "Nicht ganz. Pr\u00fcfe:", ["en"] = "Not quite. Check:" },
        ["invalidInput"] = new() { ["de"] = "Bitte g\u00fcltige Zahlen eingeben (z.B. 3, -1.5 oder 3/2).", ["en"] = "Please enter valid numbers for all parameters (e.g. 3, -1.5, or 3/2)." },
        ["tryDifferent"] = new() { ["de"] = "Werte (versuche eine andere \u00e4quivalente Form)", ["en"] = "values (try a different equivalent form)" },
    };

    private string T(string key) => I18n.TryGetValue(key, out var d) && d.TryGetValue(_lang, out var v) ? v : key;

    [ObservableProperty] private string _windowTitle = "";
    [ObservableProperty] private string _headerTitle = "";
    [ObservableProperty] private string _headerSubtitle = "";
    [ObservableProperty] private string _lblShowFormula = "";
    [ObservableProperty] private string _lblBtnCheck = "";
    [ObservableProperty] private string _lblBtnNew = "";
    [ObservableProperty] private string _lblBtnSolution = "";
    [ObservableProperty] private string _lblMeasurement = "";
    [ObservableProperty] private string _lblDragHint = "";
    [ObservableProperty] private string _lblScore = "";

    public SineParameters? CurrentParameters { get; private set; }

    public Action<SineParameters>? OnNewProblem { get; set; }

    public MainViewModel(ISineCurveService curveService)
    {
        _curveService = curveService;
        RefreshLabels();
    }

    [RelayCommand]
    private void SetLanguage(string lang)
    {
        _lang = lang;
        RefreshLabels();
        RefreshScoreText();
    }

    private void RefreshLabels()
    {
        WindowTitle = T("title");
        HeaderTitle = T("title");
        HeaderSubtitle = T("subtitle");
        LblShowFormula = T("showFormula");
        LblBtnCheck = T("btnCheck");
        LblBtnNew = T("btnNew");
        LblBtnSolution = T("btnSolution");
        LblMeasurement = T("measurement");
        LblDragHint = T("dragHint");
        LblScore = T("score");
    }

    private void RefreshScoreText()
    {
        if (string.IsNullOrEmpty(Feedback)) return;
        if (FeedbackColor == "#10B981")
            Feedback = $"{T("correct")}  ({Score} / {TotalAttempts})";
        else if (FeedbackColor == "#EF4444" && !Feedback.Contains("(e.g.") && !Feedback.Contains("(z.B."))
            Feedback = $"{T("notQuite")} ...  ({Score} / {TotalAttempts})";
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
        IsCheckEnabled = true;
        OnNewProblem?.Invoke(CurrentParameters);
    }

    [RelayCommand]
    private void CheckAnswer()
    {
        if (CurrentParameters is null || !IsCheckEnabled) return;

        if (!TryParseInput(UserA, out double a) ||
            !TryParseInput(UserB, out double b) ||
            !TryParseInput(UserC, out double c) ||
            !TryParseInput(UserD, out double d))
        {
            Feedback = T("invalidInput");
            FeedbackColor = "#EF4444";
            return;
        }

        TotalAttempts++;

        if (FunctionsMatch(a, b, c, d, CurrentParameters))
        {
            Score++;
            Feedback = $"{T("correct")}  ({Score} / {TotalAttempts})";
            FeedbackColor = "#10B981";
            IsCheckEnabled = false;
            return;
        }

        var wrong = new List<string>();
        if (Math.Abs(a - CurrentParameters.A) > 0.05) wrong.Add("a");
        if (Math.Abs(b - CurrentParameters.B) > 0.05) wrong.Add("b");
        if (Math.Abs(c - CurrentParameters.C) > 0.05) wrong.Add("c");
        if (Math.Abs(d - CurrentParameters.D) > 0.05) wrong.Add("d");

        if (wrong.Count == 0)
            wrong.Add(T("tryDifferent"));

        Feedback = $"{T("notQuite")} {string.Join(", ", wrong)}  ({Score} / {TotalAttempts})";
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
