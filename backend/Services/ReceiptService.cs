using System.Net.Http;
using System.Text.Json;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
namespace backend;
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
    
    public async Task<string> GetRawFromProverkachekaAsync(string qrCode, CancellationToken ct = default)
    {
        var client = _clientFactory.CreateClient();
        var response = await client.GetAsync($"https://proverkacheka.ru/api/v1/checks/{qrCode}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }


    public async Task<Receipt> ParseAndSaveAsync(int userId, string qrRaw, CancellationToken ct = default)
    {
        Console.WriteLine("ParseAndSaveAsync: start");
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
        //var json1 = await resp.Content.ReadAsStringAsync(ct);
        Console.WriteLine("Before categorize, items: " + receipt.Items.Count);
        await CategorizeReceiptItemsAsync(receipt.Items, userId);  
        Console.WriteLine("Before SaveChanges");
        _db.Receipts.Add(receipt);
        await _db.SaveChangesAsync(ct);
        
        Console.WriteLine("After SaveChanges, receipt id: " + receipt.Id);
        return receipt;
        
    }
public async Task CategorizeReceiptItemsAsync(List<ReceiptItem> items, int userId)
{
    var httpClient = _clientFactory.CreateClient();
   // var categories = new[] { "еда", "транспорт", "развлечения", "одежда", "медицина", "быт", "другое" };
    var categories = await GetOrCreateUserCategoriesAsync(userId);
    var itemsForPrompt = items.Select(i => new { name = i.Name }).ToArray();
    var itemsJson = JsonSerializer.Serialize(itemsForPrompt);
    
    // Промпт для gemma3:4b
    var prompt = $@"Ты классификатор товаров из чеков. 
ВЫБИРАЙ ТОЛЬКО ИЗ списка: {string.Join(", ", categories)}.

Товары: {itemsJson}

Ответь ТОЛЬКО валидным JSON (без доп текста) по образцу:
{{
  ""товары"": [
    {{""name"": ""{itemsForPrompt[0].name}"", ""category"": ""(категория товара из списка)""}}
  ]
}}";

    // Запрос к Ollama
    var requestBody = new
    {
        model = "gemma3:4b",
        prompt = prompt,
        format = "json",
        stream = false
    };
    
    var content = new StringContent(
        JsonSerializer.Serialize(requestBody), 
        Encoding.UTF8, 
        "application/json");
    
    var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));  // 60 сек таймаут
    
    try
    {{
        var response = await httpClient.PostAsync(
            "http://localhost:11434/api/generate", 
            content, 
            timeoutCts.Token);
        
        if (response.IsSuccessStatusCode)
        {{
            var ollamaJson = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            var ollamaResponse = JsonSerializer.Deserialize<OllamaApiResponse>(ollamaJson);
            
            if (!string.IsNullOrEmpty(ollamaResponse?.Response))
            {{
                var categorizationResult = JsonSerializer.Deserialize<CategorizationResult>(ollamaResponse.Response);
                
                // Применяем категории к items
                if (categorizationResult?.Products != null)
                {{
                    foreach (var catItem in categorizationResult.Products)
                    {{
                        var receiptItem = items.FirstOrDefault(i => 
                            i.Name.Contains(catItem.Name, StringComparison.OrdinalIgnoreCase));
                        
                        if (receiptItem != null)
                        {{
                            receiptItem.Category = catItem.Category ?? "другое";
                        }}
                    }}
                }}
            }}
        }}
    }}
    catch (Exception ex)
    {{
        // Fallback: все в 'другое'
        foreach (var item in items)
        {{
            item.Category ??= "другое";
        }}
        // Лог ошибки (опционально)
        Console.WriteLine($"Категоризация Ollama упала: {ex.Message}");
    }}
}

private async Task<string[]> GetOrCreateUserCategoriesAsync(int userId, CancellationToken ct = default)
{
    var userCats = await _db.UserCategories
        .Where(c => c.UserId == userId)
        .ToListAsync(ct);

    if (userCats.Count == 0)
    {
        foreach (var name in DefaultCategories.DefaultCategoryNames)
        {
            _db.UserCategories.Add(new UserCategory
            {
                UserId = userId,
                Name = name,
                IsDefault = true   // стартовый набор
            });
        }

        await _db.SaveChangesAsync(ct);
        return DefaultCategories.DefaultCategoryNames;
    }

    // Обязательно наличие "другое"
    if (!userCats.Any(c => c.Name.Equals("другое", StringComparison.OrdinalIgnoreCase)))
    {
        var other = new UserCategory
        {
            UserId = userId,
            Name = "другое",
            IsDefault = true
        };
        _db.UserCategories.Add(other);
        await _db.SaveChangesAsync(ct);
        userCats.Add(other);
    }

    // Лимит 10: если больше — обрежем до 9 + "другое"
    // (если хочешь жёстко запрещать создание — сделаем отдельный endpoint с 400)
    var custom = userCats.Where(c => !c.IsDefault).Take(9).ToList();
    var result = custom.Select(c => c.Name).ToList();
    if (!result.Contains("другое", StringComparer.OrdinalIgnoreCase))
        result.Add("другое");

    return result.ToArray();
}


}
