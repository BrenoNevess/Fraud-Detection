namespace FraudDetection.Api.Models
{
    /// <summary>
    /// Resultado da análise devolvido em JSON. RiskLevel e Decision são enviados
    /// como texto (ex.: "HighRisk", "Block") para o cliente mapear sem ambiguidade.
    /// </summary>
    public class FraudAnalysisResponse
    {
        public string RiskLevel { get; set; } = "";
        public string Decision { get; set; } = "";
        public IReadOnlyList<string> Alerts { get; set; } = new List<string>();
        public string? BlockReason { get; set; }
    }
}
