using StockFlow.Core;
using StockFlow.Core.Dtos;
using StockFlow.Core.Services;

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration["Database:ConnectionString"]
    ?? throw new InvalidOperationException("Database:ConnectionString 설정이 없습니다.");

builder.Services.AddStockFlowCore(connectionString);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();
