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
        await CategorizeReceiptItemsAsync(receipt.Items, userId, ct);  
        Console.WriteLine("Before SaveChanges");
        _db.Receipts.Add(receipt);
        await _db.SaveChangesAsync(ct);
        
        Console.WriteLine("After SaveChanges, receipt id: " + receipt.Id);
        return receipt;
        
    }
    public async Task CategorizeReceiptItemsAsync(List<ReceiptItem> items, int userId, CancellationToken ct = default)
    {
        if (items == null || items.Count == 0) return;

        var categories = await GetOrCreateUserCategoriesAsync(userId, ct);

        var itemsForPrompt = items.Select(i => new { name = i.Name }).ToArray();
        var itemsJson = JsonSerializer.Serialize(itemsForPrompt);

        var prompt =
            $@"Ты классификатор товаров из чеков.
Категории: {string.Join(", ", categories)}.

Товары (JSON-массив объектов с полем name):
{itemsJson}

Ответь ТОЛЬКО валидным JSON вида:
{{
  ""Products"": [
    {{""Name"": ""..."", ""Category"": ""одна_из_категорий""}}
  ]
}}";

        var rawContent = await CallDeepseekAsync(prompt, ct);
        Console.WriteLine("Raw content Example:");
        Console.WriteLine(rawContent);
        var jsonOnly   = ExtractJson(rawContent);

        var result = JsonSerializer.Deserialize<CategorizationResult>(jsonOnly);

        if (result?.Products == null) return;

        foreach (var catItem in result.Products)
        {
            var receiptItem = items.FirstOrDefault(i =>
                i.Name.Contains(catItem.Name, StringComparison.OrdinalIgnoreCase));

            if (receiptItem != null)
                receiptItem.Category = string.IsNullOrWhiteSpace(catItem.Category)
                    ? "другое"
                    : catItem.Category;
        }
    }



//Созданмие пользовательских категорий
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




//ПРИЗЫВ ДИПСИКА ДРЕВЛЯНАМИ
public async Task<string> CallDeepseekAsync(string userPrompt, CancellationToken ct = default)
{
    var client = _clientFactory.CreateClient();

    var body = new
    {
        model = "deepseek/deepseek-r1-0528:free",
        messages = new[]
        {
            new { role = "user", content = userPrompt }
        }
    };

    var json = JsonSerializer.Serialize(body);
    using var request = new HttpRequestMessage(
        HttpMethod.Post,
        "https://openrouter.ai/api/v1/chat/completions")
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    request.Headers.Add("Authorization", $"Bearer {_config["OpenRouter:ApiKey"]}");
    request.Headers.Add("HTTP-Referer", "https://checkchecker.local");
    request.Headers.Add("X-Title", "CheckChecker");

    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

    using var response = await client.SendAsync(request, linkedCts.Token);
    response.EnsureSuccessStatusCode();

    var responseJson = await response.Content.ReadAsStringAsync(linkedCts.Token);

    using var doc = JsonDocument.Parse(responseJson);
    var content = doc.RootElement
        .GetProperty("choices")[0]
        .GetProperty("message")
        .GetProperty("content")
        .GetString();
    return content;
}

//Извлечём джсон из Оупенроутер resppnse
private static string ExtractJson(string content)
{
    if (string.IsNullOrWhiteSpace(content))
        throw new InvalidOperationException("Пустой ответ модели");

    content = content.Trim();

    // 1) если есть <answer>...</answer> — берём то, что внутри
    var answerStart = content.IndexOf("<answer>", StringComparison.OrdinalIgnoreCase);
    var answerEnd   = content.IndexOf("</answer>", StringComparison.OrdinalIgnoreCase);
    if (answerStart >= 0 && answerEnd > answerStart)
    {
        var inner = content.Substring(
            answerStart + "<answer>".Length,
            answerEnd - (answerStart + "<answer>".Length));
        content = inner.Trim();
    }

    // 2) убираем markdown-код-блоки ```...``` и ```json ... ```
    if (content.StartsWith("```"))
    {
        // срезаем первую строку ``` или ```json
        var firstNewLine = content.IndexOf('\n');
        if (firstNewLine > 0)
            content = content.Substring(firstNewLine + 1);

        // срезаем завершающее ```
        var lastTicks = content.LastIndexOf("```", StringComparison.Ordinal);
        if (lastTicks >= 0)
            content = content.Substring(0, lastTicks);
        content = content.Trim();
    }

    // 3) финальный трим
    content = content.Trim();

    // минимальная проверка
    if (!content.StartsWith("{") && !content.StartsWith("["))
        throw new InvalidOperationException($"Ответ не похож на JSON: '{content[..Math.Min(50, content.Length)]}...'");

    return content;
}
}
