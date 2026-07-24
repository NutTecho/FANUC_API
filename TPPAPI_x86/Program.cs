using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 🎯 บังคับให้ใช้ชื่อ Property ตรงตามที่เราเขียนใน C# Class เป๊ะๆ ไม่ต้องแปลงเป็นตัวเล็ก
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseRouting();
app.UseCors(builder => 
{
    builder
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowAnyOrigin();
});


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
