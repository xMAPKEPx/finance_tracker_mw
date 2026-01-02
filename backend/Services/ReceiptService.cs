using System.Net.Http;
using System.Text.Json;

public interface IReceiptService
{
    Task<string> GetRawJsonAsync(string qrRaw, CancellationToken ct = default);
}

public class ReceiptService : IReceiptService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _config;

    public ReceiptService(IHttpClientFactory clientFactory, IConfiguration config)
    {
        _clientFactory = clientFactory;
        _config = config;
    }

    public async Task<string> GetRawJsonAsync(string qrRaw, CancellationToken ct = default)
    {
        var client = _clientFactory.CreateClient("Proverkacheka");
        var url = _config["Proverkacheka:BaseUrl"];
        var token = _config["Proverkacheka:Token"];

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(token!), "token");
        form.Add(new StringContent(qrRaw), "qrraw");

        var resp = await client.PostAsync(url, form, ct);
        resp.EnsureSuccessStatusCode();

        return await resp.Content.ReadAsStringAsync(ct);
    }
}