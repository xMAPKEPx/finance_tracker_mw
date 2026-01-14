using System.Net.Http;
using System.Text.Json;

public interface IReceiptService
{
    Task<string> GetRawFromProverkachekaAsync(string qrRaw, CancellationToken ct = default);
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

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        //вайб+код
// 1. Читаем «сырой» корень, чтобы понять, ошибка это или успех
        var rawRoot = JsonSerializer.Deserialize<ProverkachekaRootRaw>(json, options)
                      ?? throw new InvalidOperationException("Empty response from proverkacheka");

// 2. Если код != 1 — это ошибка сервиса, не пытаемся парсить чек
        if (rawRoot.Code != 1)
        {
            string errorMessage;

            if (rawRoot.Data.ValueKind == JsonValueKind.String)
                errorMessage = rawRoot.Data.GetString() ?? "Unknown error";
            else
                errorMessage = rawRoot.Data.ToString();

            throw new InvalidOperationException(
                $"Proverkacheka error (code {rawRoot.Code}): {errorMessage}");
        }

// 3. Тут уже точно успешный ответ → парсим в твою старую модель
        var root = JsonSerializer.Deserialize<ProverkachekaRoot>(json, options)
                   ?? throw new InvalidOperationException("Invalid success response from proverkacheka");

        var j = root.Data.Json;

// дальше твой код без изменений
        var totalRub = j.TotalSum / 100m;
        var dt = DateTime.Parse(j.DateTime, null, System.Globalization.DateTimeStyles.AssumeLocal);

        var receipt = new Receipt
        {
            UserId = userId,
            QrRaw = qrRaw,
            TotalSum = totalRub,
            DateTime = dt,
            StoreName = j.User
        };

        foreach (var it in j.Items)
        {
            var item = new ReceiptItem
            {
                Name = it.Name,
                Price = it.Price / 100m,
                Quantity = it.Quantity,
                Sum = it.Sum / 100m
            };
            receipt.Items.Add(item);
        }

        _db.Receipts.Add(receipt);
        await _db.SaveChangesAsync(ct);

        return receipt;
    }


    public async Task<string> GetRawFromProverkachekaAsync(string qrRaw, CancellationToken ct = default)
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
        return json;
    }
}
