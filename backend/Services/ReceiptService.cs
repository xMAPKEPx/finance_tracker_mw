using System.Net.Http;
using System.Text.Json;

public interface IReceiptService
{
    Task<Receipt> ParseAndSaveAsync(int userId, string qrRaw, CancellationToken ct = default);
}


public class ReceiptService : IReceiptService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;

    public ReceiptService(
        IHttpClientFactory clientFactory,
        IConfiguration config,
        AppDbContext db)
    {
        _clientFactory = clientFactory;
        _config = config;
        _db = db;
        
    }

    public async Task<Receipt> ParseAndSaveAsync(int userId, string qrRaw, CancellationToken ct = default)
    {
        var client = _clientFactory.CreateClient("Proverkacheka");
        var url = _config["Proverkacheka:BaseUrl"];
        var token = _config["Proverkacheka:Token"];

        using var form = new MultipartFormDataContent
        {
            { new StringContent(token!), "token" },
            { new StringContent(qrRaw), "qrraw" }
        };

        var resp = await client.PostAsync(url, form, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);

        // TODO: распарсить json в свою модель
        // Пока сделаем минимально: заполним сумму/дату "заглушками" или вытащим базовые поля.
        var receipt = new Receipt
        {
            UserId = userId,
            QrRaw = qrRaw,
            TotalSum = 0,                  // сюда потом положишь реальную сумму
            DateTime = DateTime.UtcNow     // сюда — реальное время из json
        };

        _db.Receipts.Add(receipt);
        await _db.SaveChangesAsync(ct);

        return receipt;
    }
}
