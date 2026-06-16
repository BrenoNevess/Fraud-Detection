using FraudDetection.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers (endpoints REST) + Swagger para documentação/testes.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Lógica de análise: stateless e pura, registrada como singleton.
builder.Services.AddSingleton<FraudAnalyzer>();

var app = builder.Build();

// Swagger UI disponível na raiz (http://localhost:5133).
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Fraud Detection API v1");
    options.RoutePrefix = string.Empty;
});

app.MapControllers();

app.Run();
