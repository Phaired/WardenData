namespace WardenData.DTOs;

public class RuneSuccessStatsDto
{
    public int RuneId { get; set; }
    public string RuneName { get; set; } = string.Empty;
    public string EffectName { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int Successes { get; set; }
    public double SuccessRate { get; set; }
    public bool IsTenta { get; set; }
    public double AverageGain { get; set; }
}

public class EffectProgressionDto
{
    public Guid SessionId { get; set; }
    public int RuneId { get; set; }
    public string EffectName { get; set; } = string.Empty;
    public int CurrentValue { get; set; }
    public int DesiredValue { get; set; }
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public bool HasSucceed { get; set; }
    public DateTime Timestamp { get; set; }
}

public class CostEfficiencyDto
{
    public Guid SessionId { get; set; }
    public string OrderName { get; set; } = string.Empty;
    public int TotalRunesUsed { get; set; }
    public int TotalSuccesses { get; set; }
    public long TotalCost { get; set; }
    public double SuccessRate { get; set; }
    public double CostPerSuccess { get; set; }
    public DateTime SessionDate { get; set; }
}

public class TentaComparisonDto
{
    public string EffectName { get; set; } = string.Empty;
    public bool IsTenta { get; set; }
    public int TotalAttempts { get; set; }
    public int Successes { get; set; }
    public double SuccessRate { get; set; }
}

public class SessionProgressDto
{
    public Guid SessionId { get; set; }
    public string OrderName { get; set; } = string.Empty;
    public List<ProgressStepDto> Steps { get; set; } = new();
    public DateTime SessionDate { get; set; }
    public int TotalSteps { get; set; }
    public bool IsCompleted { get; set; }
}

public class ProgressStepDto
{
    public int StepOrder { get; set; }
    public int RuneId { get; set; }
    public string RuneName { get; set; } = string.Empty;
    public bool IsTenta { get; set; }
    public bool HasSucceed { get; set; }
    public List<EffectChangeDto> EffectChanges { get; set; } = new();
}

public class EffectChangeDto
{
    public string EffectName { get; set; } = string.Empty;
    public int PreviousValue { get; set; }
    public int CurrentValue { get; set; }
    public int DesiredValue { get; set; }
    public int Change { get; set; }
}

public class RuneUsageStatsDto
{
    public int RuneId { get; set; }
    public string RuneName { get; set; } = string.Empty;
    public string EffectName { get; set; } = string.Empty;
    public double Weight { get; set; }
    public int RuneValue { get; set; }
    public int? PaRuneValue { get; set; }
    public int? RaRuneValue { get; set; }
    public int TotalUsage { get; set; }
    public double OverallSuccessRate { get; set; }
}