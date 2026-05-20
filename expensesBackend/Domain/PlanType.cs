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
        PlanType.Starter => 100,
        _                => 300
    };
}
