using System.Net.Http.Json;
using FraudDetection.Web.Models.Enums;
using FraudDetection.Web.Services.Interfaces;

namespace FraudDetection.Web.Services
{
    /// <summary>
    /// Consome a API externa de análise de fraude via HTTP. Implementa
    /// <see cref="IFraudDetectionService"/>: para o resto do app nada muda — a
    /// diferença é que a decisão agora vem da API, e não de lógica local.
    /// </summary>
    public class FraudApiClient : IFraudDetectionService
    {
        private readonly HttpClient _http;

        public FraudApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<FraudAnalysisResult> AnalyzeAsync(
            decimal amount,
            DateTime when,
            string? transactionLocation,
            string? userLocation)
        {
            // 1. Monta o corpo da requisição e faz o POST para a API.
            var request = new
            {
                Amount = amount,
                When = when,
                TransactionLocation = transactionLocation,
                UserLocation = userLocation
            };

            HttpResponseMessage response =
                await _http.PostAsJsonAsync("api/fraud/analyze", request);

            response.EnsureSuccessStatusCode();

            // 2. Lê o JSON de resposta.
            ApiResponse? body = await response.Content.ReadFromJsonAsync<ApiResponse>();

            if (body is null)
            {
                throw new InvalidOperationException("Resposta vazia da API de fraude.");
            }

            // 3. Converte os textos da API de volta para os enums do app.
            FraudRiskLevel risk = Enum.Parse<FraudRiskLevel>(body.RiskLevel, ignoreCase: true);
            FraudDecision decision = Enum.Parse<FraudDecision>(body.Decision, ignoreCase: true);

            return new FraudAnalysisResult(risk, decision, body.Alerts, body.BlockReason);
        }

        // Espelho do JSON devolvido pela API (FraudAnalysisResponse).
        private sealed class ApiResponse
        {
            public string RiskLevel { get; set; } = "";
            public string Decision { get; set; } = "";
            public List<string> Alerts { get; set; } = new();
            public string? BlockReason { get; set; }
        }
    }
}
