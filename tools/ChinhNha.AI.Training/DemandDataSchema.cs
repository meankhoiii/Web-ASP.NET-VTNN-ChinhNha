namespace ChinhNha.AI.Training;

/// <summary>
/// Input data schema for ML.NET SSA Time Series model.
/// Each row represents one weekly demand record.
/// </summary>
public class DemandRow
{
    public float WeeklyDemand { get; set; }
}

/// <summary>
/// Prediction output from the ML.NET SSA model.
/// </summary>
public class DemandForecast
{
    public float[] ForecastedValues { get; set; } = Array.Empty<float>();
    public float[] LowerBound { get; set; } = Array.Empty<float>();
    public float[] UpperBound { get; set; } = Array.Empty<float>();
}
