using FraudDetection.Api.Models;

namespace FraudDetection.Api.Services
{
    /// <summary>
    /// Resultado interno da análise (com enums). O controller o converte para
    /// <see cref="FraudAnalysisResponse"/> antes de enviar em JSON.
    /// </summary>
    public record FraudAnalysisOutcome(
        FraudRiskLevel RiskLevel,
        FraudDecision Decision,
        IReadOnlyList<string> Alerts,
        string? BlockReason);
}
