
public class Receipt
{
    public int Id { get; set; }              // PK
    public string QrRaw { get; set; } = null!;
    public decimal TotalSum { get; set; }
    public DateTime DateTime { get; set; }
    public string? StoreName { get; set; }
    public List<ReceiptItem> Items { get; set; } = new();

    public int UserId { get; set; }          // FK
    public User User { get; set; } = null!;
}