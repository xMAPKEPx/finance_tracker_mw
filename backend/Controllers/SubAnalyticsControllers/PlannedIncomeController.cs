using backend;
using backend.Infrastructure;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlannedIncomeController : ControllerBase
{
    private readonly AppDbContext _context;

    public PlannedIncomeController(AppDbContext context)
    {
        _context = context;
    }

    public class UpsertMonthlyPlannedIncomeRequest
    {
        public int Year { get; set; }
        public int Month { get; set; }

        public decimal PlannedAmount { get; set; }

        public bool SaveAsRecurring { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] UpsertMonthlyPlannedIncomeRequest request)
    {
        var userId = User.GetUserId();

        var monthly = await _context.MonthlyPlannedIncomes
            .SingleOrDefaultAsync(x =>
                x.UserId == userId &&
                x.Year == request.Year &&
                x.Month == request.Month);

        if (monthly is null)
        {
            monthly = new MonthlyPlannedIncome
            {
                UserId = userId,
                Year = request.Year,
                Month = request.Month,
                PlannedAmount = request.PlannedAmount
            };
            _context.MonthlyPlannedIncomes.Add(monthly);
        }
        else
        {
            monthly.PlannedAmount = request.PlannedAmount;
        }

        if (request.SaveAsRecurring)
        {
            var recurring = await _context.RecurringIncomes
                .SingleOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Category == null);

            if (recurring is null)
            {
                recurring = new RecurringIncome
                {
                    UserId = userId,
                    Amount = request.PlannedAmount,
                    Category = null,
                    Description = "Плановый доход",
                    IsActive = true
                };
                _context.RecurringIncomes.Add(recurring);
            }
            else
            {
                recurring.Amount = request.PlannedAmount;
                recurring.IsActive = true;
            }
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<MonthlyPlannedIncome?>> Get(int year, int month)
    {
        var userId = User.GetUserId();

        var monthly = await _context.MonthlyPlannedIncomes
            .SingleOrDefaultAsync(x =>
                x.UserId == userId &&
                x.Year == year &&
                x.Month == month);

        return Ok(monthly);
    }
}
