using backend;
using backend.Infrastructure;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlannedExpensesController : ControllerBase
{
    private readonly AppDbContext _context;

    public PlannedExpensesController(AppDbContext context)
    {
        _context = context;
    }

    public class UpsertMonthlyPlannedExpenseRequest
    {
        public int Year { get; set; }
        public int Month { get; set; }

        public string CategoryName { get; set; }
        public decimal PlannedAmount { get; set; }

        public bool SaveAsRecurring { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] UpsertMonthlyPlannedExpenseRequest request)
    {
        var userId = User.GetUserId();

        var monthly = await _context.MonthlyPlannedExpenses
            .SingleOrDefaultAsync(x =>
                x.UserId == userId &&
                x.Year == request.Year &&
                x.Month == request.Month &&
                x.CategoryName == request.CategoryName);

        if (monthly is null)
        {
            monthly = new MonthlyPlannedExpense
            {
                UserId = userId,
                Year = request.Year,
                Month = request.Month,
                CategoryName = request.CategoryName,
                PlannedAmount = request.PlannedAmount
            };
            _context.MonthlyPlannedExpenses.Add(monthly);
        }
        else
        {
            monthly.PlannedAmount = request.PlannedAmount;
        }

        if (request.SaveAsRecurring)
        {
            var recurring = await _context.RecurringPlannedExpenses
                .SingleOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.CategoryName == request.CategoryName);

            if (recurring is null)
            {
                recurring = new RecurringPlannedExpense
                {
                    UserId = userId,
                    CategoryName = request.CategoryName,
                    PlannedAmount = request.PlannedAmount,
                    IsActive = true
                };
                _context.RecurringPlannedExpenses.Add(recurring);
            }
            else
            {
                recurring.PlannedAmount = request.PlannedAmount;
                recurring.IsActive = true;
            }
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    // Все плановые расходы по категориям за месяц
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MonthlyPlannedExpense>>> Get(int year, int month)
    {
        var userId = User.GetUserId();

        var items = await _context.MonthlyPlannedExpenses
            .Include(x => x.Category)
            .Where(x =>
                x.UserId == userId &&
                x.Year == year &&
                x.Month == month)
            .ToListAsync();

        return Ok(items);
    }
}
