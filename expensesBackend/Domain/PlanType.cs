namespace ExpensesBackend.API.Domain;

public enum PlanType { Free, Starter, Pro }

public static class PlanLimits
{
    public static int MaxBooks(PlanType plan) => plan == PlanType.Free ? 3 : int.MaxValue;

    public static int MaxExpensesPerMonth(PlanType plan) => plan switch
    {
        PlanType.Free    => 150,
        PlanType.Starter => 1000,
        _                => int.MaxValue
    };

    public static int MaxCategories(PlanType plan) => plan switch
    {
        PlanType.Free    => 20,
        PlanType.Starter => 50,
        _                => int.MaxValue
    };

    public static int MonthlyCredits(PlanType plan) => plan switch
    {
        PlanType.Free    => 0,
        PlanType.Starter => 50,
        _                => 150
    };

    /// <summary>
    /// Number of free Auto-classify uses (button press or AI-on-create) before credits are charged.
    /// Free plan: 5 lifetime; Starter/Pro: 15 per month (resets with monthly credits).
    /// </summary>
    public static int AutoClassifyFreeQuota(PlanType plan) => plan == PlanType.Free ? 5 : 15;
}
