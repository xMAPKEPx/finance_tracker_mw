using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReceiptsController : ControllerBase
{
    private readonly IReceiptService _receiptService;

    public ReceiptsController(IReceiptService receiptService)
    {
        _receiptService = receiptService;
    }
    
    [HttpPost("parse")]
    public async Task<ActionResult<Receipt>> Parse([FromBody] ParseReceiptRequest request)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (userIdString is null)
                        return Unauthorized();
            
                    var userId = int.Parse(userIdString);
            
                    var receipt = await _receiptService.ParseAndSaveAsync(userId, request.QrRaw);
                    return Ok(receipt);
        }
        catch (InvalidOperationException ex)
        {
            // сюда прилетят ошибки вида "Proverkacheka error (code ...): ..."
            return BadRequest(new { error = ex.Message });
        }
        
    }

    [HttpGet("user/Receipts")]
    public async Task<ActionResult<List<Receipt>>> GetByUser([FromServices] AppDbContext db, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var receipts = await db.Receipts
            .Where(r => r.UserId == userId)
            .Include(r => r.Items) //чтобы айтемы показывались
            .OrderByDescending(r => r.DateTime)
            .ToListAsync(ct);

        return receipts;
    }
    [HttpPost("debug-raw")]
    public async Task<ActionResult<string>> DebugRaw([FromBody] ParseReceiptRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.QrRaw))
            return BadRequest("QrRaw is required.");

        var json = await _receiptService.GetRawFromProverkachekaAsync(request.QrRaw, ct);
        return Content(json, "application/json");
    }
}
