using backend.Infrastructure;
using backend.Models;
using backend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AnalyticsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("monthly")]
    public async Task<ActionResult<MonthlyAnalyticsResponse>> GetMonthly(int year, int month)
    {
        var userId = User.GetUserId();

        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        // 1. Бюджет
        var budget = await _context.MonthlyBudgets
            .SingleOrDefaultAsync(b =>
                b.UserId == userId &&
                b.Year == year &&
                b.Month == month);

        var initialBudget = budget?.InitialAmount ?? 0m;

        // 2. Плановый доход
        var plannedIncome = await _context.MonthlyPlannedIncomes
            .Where(x => x.UserId == userId && x.Year == year && x.Month == month)
            .Select(x => x.PlannedAmount)
            .SingleOrDefaultAsync();

        // 3. Фактические доходы
        var incomes = await _context.Incomes
            .Where(x =>
                x.UserId == userId &&
                x.Date >= start &&
                x.Date < end)
            .ToListAsync();

        var actualIncome = incomes.Sum(x => x.Amount);

        // 4. Фактические расходы по чекам (предполагаем, что у тебя есть таблица Receipts)
        var receipts = await _context.Receipts
            .Where(x =>
                x.UserId == userId &&
                x.DateTime >= start &&
                x.DateTime < end)
            .ToListAsync();

        var actualExpensesTotal = receipts.Sum(x => x.TotalSum); // подстроить под твоё поле суммы

        // Позиции чеков за месяц
var receiptItems = await _context.ReceiptItems
    .Include(x => x.Receipt)
    .Where(x =>
        x.Receipt.UserId == userId &&
        x.Receipt.DateTime >= start &&
        x.Receipt.DateTime < end)
    .ToListAsync();

// Группировка по строковому Category
var actualByCategory = receiptItems
    .GroupBy(x => x.Category ?? "Без категории")
    .Select(g => new
    {
        CategoryName = g.Key,
        Actual = g.Sum(i => i.Sum)
    })
    .ToList();

// Плановые расходы по категориям у тебя сейчас завязаны на CategoryId,
// но раз в чеке только строки, можно пока тоже хранить план по имени категории:
var plannedByCategory = await _context.MonthlyPlannedExpenses
    .Where(x =>
        x.UserId == userId &&
        x.Year == year &&
        x.Month == month)
    .ToListAsync();

// допустим, в MonthlyPlannedExpense вместо CategoryId сейчас есть CategoryName (string)
var byCategory = new List<CategoryAnalyticsItem>();

foreach (var p in plannedByCategory)
{
    var actual = actualByCategory
        .FirstOrDefault(a => a.CategoryName == p.CategoryName);

    var actualValue = actual?.Actual ?? 0m;

    byCategory.Add(new CategoryAnalyticsItem
    {
        CategoryId = 0, // временно, пока нет числового Id
        CategoryName = p.CategoryName,
        Planned = p.PlannedAmount,
        Actual = actualValue,
        Difference = actualValue - p.PlannedAmount
    });
}

// категории, у которых есть только факт
foreach (var a in actualByCategory)
{
    if (!byCategory.Any(x => x.CategoryName == a.CategoryName))
    {
        byCategory.Add(new CategoryAnalyticsItem
        {
            CategoryId = 0,
            CategoryName = a.CategoryName,
            Planned = 0m,
            Actual = a.Actual,
            Difference = a.Actual
        });
    }
}


        // 7. Дневные серии для графика
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var dates = new List<DateTime>();
        var dailyExpenses = new List<decimal>();
        var dailyIncomes = new List<decimal>();

        for (int day = 1; day <= daysInMonth; day++)
        {
            var d = new DateTime(year, month, day);
            dates.Add(d);

            var exp = receipts
                .Where(r => r.DateTime.Date == d.Date)
                .Sum(r => r.TotalSum);

            var inc = incomes
                .Where(i => i.Date.Date == d.Date)
                .Sum(i => i.Amount);

            dailyExpenses.Add(exp);
            dailyIncomes.Add(inc);
        }

        var currentBudget = initialBudget - actualExpensesTotal + actualIncome;
        var plannedExpensesTotal = plannedByCategory.Sum(x => x.PlannedAmount);
        var response = new MonthlyAnalyticsResponse
        {
            Year = year,
            Month = month,
            InitialBudget = initialBudget,
            PlannedIncome = plannedIncome,
            ActualIncome = actualIncome,
            PlannedExpensesTotal = plannedExpensesTotal,
            ActualExpensesTotal = actualExpensesTotal,
            CurrentBudget = currentBudget,
            ByCategory = byCategory,
            DailySeries = new DailySeries
            {
                Dates = dates,
                Expenses = dailyExpenses,
                Incomes = dailyIncomes
            }
        };

        return Ok(response);
    }
}
