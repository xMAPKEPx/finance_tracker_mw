
public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;      // красивое имя
    public string Login { get; set; } = null!;     // по нему логинимся
    public string PasswordHash { get; set; } = null!;

    public List<Receipt> Receipts { get; set; } = new();
}
