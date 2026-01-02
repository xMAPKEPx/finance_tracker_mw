
public class User
{
    public int Id { get; set; }              // PK
    public string Name { get; set; } = null!;
    public string? Email { get; set; }

    public List<Receipt> Receipts { get; set; } = new();
}