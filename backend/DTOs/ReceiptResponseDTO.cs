//------------------НЕАКТКУАЛЬНЫЙ КЛАСС!-------------------------
public class ReceiptResponseDto
{
    public int Id { get; set; }

    public string ShopName { get; set; } = null!;
    public DateTime PurchaseDate { get; set; }
    public decimal Total { get; set; }
    public List<ReceiptItem> Items { get; set; }

    // Добавь сюда те поля, которые реально нужны фронту:
    // список позиций, НДС и т.п.
} //--------------------------------------------------------
public class ParseReceiptRequest
{
    public string QrRaw { get; set; } = null!;
}