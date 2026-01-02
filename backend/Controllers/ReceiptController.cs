using Microsoft.AspNetCore.Mvc;

public class ParseReceiptRequest
{
    public string QrRaw { get; set; } = null!;
}

[ApiController]
[Route("api/[controller]")]
public class ReceiptsController : ControllerBase
{
    private readonly IReceiptService _receiptService;

    public ReceiptsController(IReceiptService receiptService)
    {
        _receiptService = receiptService;
    }

    /// <summary>
    /// Принимает строку из QR-кода и возвращает JSON от proverkacheka.
    /// </summary>
    [HttpPost("parse")]
    public async Task<ActionResult<string>> Parse([FromBody] ParseReceiptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.QrRaw))
            return BadRequest("QrRaw is required.");

        var json = await _receiptService.GetRawJsonAsync(request.QrRaw);
        return Content(json, "application/json");
    }
}