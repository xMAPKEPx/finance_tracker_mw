using Microsoft.EntityFrameworkCore;
using backend;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


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
// HttpClient для proverkacheka
builder.Services.AddHttpClient("Proverkacheka");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlite(connectionString);
});

    // Наш сервис для чеков
builder.Services.AddScoped<IReceiptService, ReceiptService>();

//Добавляем аутентификейшон бикос электростейшон 
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();
//Подрубаем авторизацию
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();


// 4. Включаем CORS
app.UseCors("AllowAll");

// 5. ВКЛЮЧАЕМ SWAGGER ВСЕГДА (убрав if)
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.UseAuthentication();   // обязательно перед UseAuthorization
app.UseAuthorization();
    
    
app.Run();

