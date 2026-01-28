using backend;
using backend.Infrastructure;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly AppDbContext _context;

    public BudgetsController(AppDbContext context)
    {
        _context = context;
    }

    // DTO
    public class UpsertMonthlyBudgetRequest
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal InitialAmount { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] UpsertMonthlyBudgetRequest request)
    {
        var userId = User.GetUserId();

        var budget = await _context.MonthlyBudgets
            .SingleOrDefaultAsync(b =>
                b.UserId == userId &&
                b.Year == request.Year &&
                b.Month == request.Month);

        if (budget is null)
        {
            budget = new MonthlyBudget
            {
                UserId = userId,
                Year = request.Year,
                Month = request.Month,
                InitialAmount = request.InitialAmount
            };
            _context.MonthlyBudgets.Add(budget);
        }
        else
        {
            budget.InitialAmount = request.InitialAmount;
        }

        await _context.SaveChangesAsync();
        return Ok(budget);
    }

    // Текущий месяц
    [HttpGet("current")]
    public async Task<ActionResult<MonthlyBudget?>> GetCurrent()
    {
        var userId = User.GetUserId();
        var now = DateTime.UtcNow;

        var budget = await _context.MonthlyBudgets
            .SingleOrDefaultAsync(b =>
                b.UserId == userId &&
                b.Year == now.Year &&
                b.Month == now.Month);

        return Ok(budget);
    }

    // Конкретный месяц
    [HttpGet]
    public async Task<ActionResult<MonthlyBudget?>> Get(int year, int month)
    {
        var userId = User.GetUserId();

        var budget = await _context.MonthlyBudgets
            .SingleOrDefaultAsync(b =>
                b.UserId == userId &&
                b.Year == year &&
                b.Month == month);

        return Ok(budget);
    }
}
