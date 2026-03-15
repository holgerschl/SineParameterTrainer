using SineParameterTrainer.Models;

namespace SineParameterTrainer.Services;

public interface ISineCurveService
{
    SineParameters GenerateRandomParameters();
}
