using SineParameterTrainer.Models;

namespace SineParameterTrainer.Services;

public class SineCurveService : ISineCurveService
{
    private readonly Random _random = new();

    public SineParameters GenerateRandomParameters()
    {
        // a: positive integer amplitude from [1, 10]
        int a = _random.Next(1, 11);

        // b: positive integer [1, 4] so period P = 2π/b is a clean fraction of π
        int b = _random.Next(1, 5);

        // c: phase shift, integer with |c| < half-period (π/b)
        int maxC = (int)Math.Floor(Math.PI / b);
        int c = maxC > 0 ? _random.Next(-maxC, maxC + 1) : 0;

        // d: vertical shift, integer in [-5, 5]
        int d = _random.Next(-5, 6);

        return new SineParameters(a, b, c, d);
    }

    private int RandomNonZeroInt(int min, int max)
    {
        int val;
        do { val = _random.Next(min, max + 1); } while (val == 0);
        return val;
    }
}
