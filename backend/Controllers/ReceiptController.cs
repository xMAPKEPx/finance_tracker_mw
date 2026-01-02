using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class ParseReceiptRequest
{
    public int UserId { get; set; }      // пока просто числом
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

    [HttpPost("parse")]
    public async Task<ActionResult<Receipt>> Parse([FromBody] ParseReceiptRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.QrRaw))
            return BadRequest("QrRaw is required.");

        var receipt = await _receiptService.ParseAndSaveAsync(request.UserId, request.QrRaw, ct);
        return Ok(receipt);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<Receipt>>> GetByUser(int userId, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var receipts = await db.Receipts
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.DateTime)
            .ToListAsync(ct);

        return receipts;
    }
    
}
