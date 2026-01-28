using backend;
using backend.Infrastructure;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IncomesController : ControllerBase
{
    private readonly AppDbContext _context;

    public IncomesController(AppDbContext context)
    {
        _context = context;
    }

    public class CreateIncomeRequest
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public bool SaveAsRecurring { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIncomeRequest request)
    {
        var userId = User.GetUserId();

        var income = new Income
        {
            UserId = userId,
            Date = request.Date,
            Amount = request.Amount,
            Category = request.Category,
            Description = request.Description
        };

        _context.Incomes.Add(income);

        if (request.SaveAsRecurring)
        {
            var recurring = await _context.RecurringIncomes
                .SingleOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Category == request.Category);

            if (recurring is null)
            {
                recurring = new RecurringIncome
                {
                    UserId = userId,
                    Amount = request.Amount,
                    Category = request.Category,
                    Description = request.Description,
                    IsActive = true
                };
                _context.RecurringIncomes.Add(recurring);
            }
            else
            {
                recurring.Amount = request.Amount;
                recurring.Description = request.Description;
                recurring.IsActive = true;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(income);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Income>>> Get(int year, int month)
    {
        var userId = User.GetUserId();

        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        var incomes = await _context.Incomes
            .Where(x =>
                x.UserId == userId &&
                x.Date >= start &&
                x.Date < end)
            .OrderBy(x => x.Date)
            .ToListAsync();

        return Ok(incomes);
    }
}
