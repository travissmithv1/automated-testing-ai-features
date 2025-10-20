namespace MetricsApi.Models;

public class HealthInsuranceFacts
{
    public string? Provider { get; set; }
    public List<string> Plans { get; set; } = new();
    public int? PPOCost { get; set; }
    public int? PPODeductible { get; set; }
    public int? HMOCost { get; set; }
    public int? HMODeductible { get; set; }
    public int? HDHPCost { get; set; }
    public int? HDHPDeductible { get; set; }
    public string? CoverageStartDay { get; set; }
    public int? DentalMaxBenefit { get; set; }
}

public class RetirementFacts
{
    public string? PlanType { get; set; }
    public int? MatchPercentage { get; set; }
    public bool? ImmediateEnrollment { get; set; }
    public bool? ImmediateVesting { get; set; }
    public int? ContributionLimitUnder50 { get; set; }
    public int? ContributionLimit50Plus { get; set; }
}

public class VacationFacts
{
    public int? AnnualDays { get; set; }
    public decimal? MonthlyAccrual { get; set; }
    public bool? IncreasesWithTenure { get; set; }
    public int? SickLeaveDays { get; set; }
    public int? PersonalDays { get; set; }
    public int? Holidays { get; set; }
}

public class ParentalLeaveFacts
{
    public int? PrimaryCaregiverWeeks { get; set; }
    public int? SecondaryCaregiverWeeks { get; set; }
    public bool? IsPaid { get; set; }
    public List<string> EligibleEvents { get; set; } = new();
}

public class LifeInsuranceFacts
{
    public string? BasicCoverageMultiplier { get; set; }
    public bool? IsBasicFree { get; set; }
    public string? SupplementalMaxMultiplier { get; set; }
}

public class FSAFacts
{
    public int? HealthcareFSALimit { get; set; }
    public int? DependentCareFSALimit { get; set; }
}
