namespace backend;

public class MonthlyAnalyticsResponse
{
    public int Year { get; set; }
    public int Month { get; set; }

    public decimal InitialBudget { get; set; }      // MonthlyBudget.InitialAmount
    public decimal PlannedIncome { get; set; }      // MonthlyPlannedIncome.PlannedAmount
    public decimal ActualIncome { get; set; }       // sum(Incomes)
    public decimal PlannedExpensesTotal { get; set; }  // sum(MonthlyPlannedExpenses)
    public decimal ActualExpensesTotal { get; set; }   // sum(Receipts)
    public decimal CurrentBudget { get; set; }      // InitialBudget - ActualExpensesTotal + ActualIncome

    public List<CategoryAnalyticsItem> ByCategory { get; set; } = new();
    public DailySeries DailySeries { get; set; } = new();
}

public class CategoryAnalyticsItem
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = default!;
    public decimal Planned { get; set; }
    public decimal Actual { get; set; }
    public decimal Difference { get; set; } // Actual - Planned
}

public class DailySeries
{
    public List<DateTime> Dates { get; set; } = new();
    public List<decimal> Expenses { get; set; } = new();
    public List<decimal> Incomes { get; set; } = new();
}

