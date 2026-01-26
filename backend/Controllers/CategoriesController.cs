using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CategoriesController(AppDbContext db)
        {
            _db = db;
        }

        // Достаём userId из JWT (ClaimTypes.NameIdentifier)
        private int GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                          ?? throw new InvalidOperationException("User id claim not found");
            return int.Parse(idClaim.Value);
        }

        // DTO для создания категории
        public class CategoryCreateDto
        {
            public string Name { get; set; } = "";
        }

        // GET: /api/categories
        // Вернуть все категории текущего пользователя
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserCategory>>> Get(CancellationToken ct)
        {
            var userId = GetUserId();

            var cats = await _db.UserCategories
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.IsDefault)   // сначала пользовательские, потом дефолтные
                .ThenBy(c => c.Name)
                .ToListAsync(ct);

            return Ok(cats);
        }

        // POST: /api/categories
        // Создать новую пользовательскую категорию (лимит 10 включая "другое")
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryCreateDto dto, CancellationToken ct)
        {
            var userId = GetUserId();

            // валидация имени
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Имя категории не может быть пустым.");

            var normalizedName = dto.Name.Trim();

            if (normalizedName.Equals("другое", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Категория 'другое' создаётся автоматически и не может быть добавлена вручную.");

            // Сколько уже есть пользовательских (не IsDefault)
            var customCount = await _db.UserCategories
                .CountAsync(c => c.UserId == userId && !c.IsDefault, ct);

            if (customCount >= 9) // 9 своих + 1 'другое' = 10
                return BadRequest("Лимит категорий: максимум 10 (включая 'другое').");

            // Проверка на дубликат
            var exists = await _db.UserCategories.AnyAsync(
                c => c.UserId == userId && c.Name == normalizedName,
                ct);

            if (exists)
                return BadRequest("Такая категория уже существует.");

            var category = new UserCategory
            {
                UserId = userId,
                Name = normalizedName,
                IsDefault = false
            };

            _db.UserCategories.Add(category);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(Get), new { id = category.Id }, category);
        }

        // DELETE: /api/categories/{id}
        // Удалить одну категорию пользователя (кроме 'другое' и дефолтных)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var userId = GetUserId();

            var cat = await _db.UserCategories
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);

            if (cat == null)
                return NotFound();

            if (cat.IsDefault || cat.Name.Equals("другое", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Нельзя удалить категорию 'другое' и дефолтные категории.");

            _db.UserCategories.Remove(cat);
            await _db.SaveChangesAsync(ct);

            return NoContent();
        }
    }
}
