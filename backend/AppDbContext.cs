using Microsoft.EntityFrameworkCore;
using backend.Models;
namespace backend;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Receipt> Receipts { get; set; } = null!;
    public DbSet<ReceiptItem> ReceiptItems { get; set; } = null!;
    public DbSet<UserCategory> UserCategories { get; set; }
   
    
    public DbSet<MonthlyBudget> MonthlyBudgets { get; set; } = default!;
    public DbSet<MonthlyPlannedExpense> MonthlyPlannedExpenses { get; set; } = default!;
    public DbSet<MonthlyPlannedIncome> MonthlyPlannedIncomes { get; set; } = default!;
    public DbSet<Income> Incomes { get; set; } = default!;
    public DbSet<RecurringPlannedExpense> RecurringPlannedExpenses { get; set; } = default!;
    public DbSet<RecurringIncome> RecurringIncomes { get; set; } = default!;
   
    // ...
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Уникальный бюджет в месяц на пользователя
        modelBuilder.Entity<MonthlyBudget>()
            .HasIndex(b => new { b.UserId, b.Year, b.Month })
            .IsUnique();
        
        modelBuilder.Entity<MonthlyPlannedExpense>()
            .HasIndex(p => new { p.UserId, p.Year, p.Month, p.CategoryName })
            .IsUnique();
        }
    }

