using Microsoft.EntityFrameworkCore;
using backend;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Services (ДО Build)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();  // Один раз

// CORS (одна политика)
builder.Services.AddCors(options =>
{
    options.AddPolicy("_frontend", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
                new Uri(origin).Host == "localhost")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
// Db, HttpClient, Services...
builder.Services.AddHttpClient("Proverkacheka");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IReceiptService, ReceiptService>();

// JWT (один раз)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware (порядок критичен!)
if (app.Environment.IsDevelopment()) {  // Или всегда для Swagger
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS ПЕРВЫЙ!
app.UseCors("_frontend");

// Auth ДО контроллеров
app.UseAuthentication();
app.UseAuthorization();

// Контроллеры ПОСЛЕДНИМИ
app.MapControllers();

app.Run();