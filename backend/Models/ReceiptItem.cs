using System.Text.Json.Serialization;
public class ReceiptItem
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    
    [JsonIgnore]   
    public Receipt Receipt { get; set; } = null!;

    public string Name { get; set; } = null!;
    public decimal Price { get; set; }      // рубли
    public decimal Quantity { get; set; }
    public decimal Sum { get; set; }        // рубли
    public string? Category { get; set; } = null!;
}
