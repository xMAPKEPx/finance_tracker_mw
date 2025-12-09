var builder = WebApplication.CreateBuilder(args);

// 1. Добавляем контроллеры
builder.Services.AddControllers();

// 2. Включаем Swagger (ДАЖЕ ЕСЛИ НЕ В РЕЖИМЕ РАЗРАБОТКИ)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Настраиваем CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// 4. Включаем CORS
app.UseCors("AllowAll");

// 5. ВКЛЮЧАЕМ SWAGGER ВСЕГДА (убрав if)
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();