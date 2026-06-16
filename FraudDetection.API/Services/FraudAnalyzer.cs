using FraudDetection.Api.Models;

namespace FraudDetection.Api.Services
{
    /// <summary>
    /// Regras de detecção de fraude por valor, horário e localização.
    /// Lógica pura (sem banco), exposta pela API e coberta por testes.
    /// O nível final é sempre o de maior severidade entre as regras acionadas.
    /// </summary>
    public class FraudAnalyzer
    {
        private const decimal HighRiskThreshold = 3000m;   // valor extremamente elevado
        private const decimal SuspiciousThreshold = 1000m; // valor acima do normal

        public FraudAnalysisOutcome Analyze(
            decimal amount,
            DateTime when,
            string? transactionLocation,
            string? userLocation)
        {
            List<string> alerts = new();
            FraudRiskLevel risk = FraudRiskLevel.Safe;

            bool extremelyHigh = amount >= HighRiskThreshold;
            bool aboveNormal = amount >= SuspiciousThreshold;
            bool oddHour = when.Hour <= 5 || when.Hour >= 23;
            bool differentLocation = IsDifferentLocation(transactionLocation, userLocation);

            if (extremelyHigh)
            {
                risk = Escalate(risk, FraudRiskLevel.HighRisk);
                alerts.Add("Valor extremamente elevado.");
            }
            else if (aboveNormal)
            {
                risk = Escalate(risk, FraudRiskLevel.Suspicious);
                alerts.Add("Valor acima do normal.");
            }

            if (oddHour)
            {
                risk = Escalate(risk, FraudRiskLevel.Suspicious);
                alerts.Add("Horário atípico para transações.");
            }

            if (differentLocation)
            {
                alerts.Add("Localização diferente da cadastrada.");
            }

            // Bloqueio automático: valor extremamente elevado OU a combinação
            // (acima do normal + horário atípico + localização diferente).
            bool blockByValue = extremelyHigh;
            bool blockByCombo = aboveNormal && oddHour && differentLocation;

            if (blockByValue || blockByCombo)
            {
                string reason = blockByValue
                    ? "Valor extremamente elevado."
                    : "Valor acima do normal em horário atípico e em localização diferente da cadastrada.";

                return new FraudAnalysisOutcome(
                    FraudRiskLevel.HighRisk,
                    FraudDecision.Block,
                    alerts,
                    reason);
            }

            FraudDecision decision = risk == FraudRiskLevel.Safe
                ? FraudDecision.Allow
                : FraudDecision.RequireConfirmation;

            return new FraudAnalysisOutcome(risk, decision, alerts, null);
        }

        private static bool IsDifferentLocation(string? transactionLocation, string? userLocation)
        {
            if (string.IsNullOrWhiteSpace(transactionLocation) ||
                string.IsNullOrWhiteSpace(userLocation))
            {
                return false;
            }

            return !string.Equals(
                transactionLocation.Trim(),
                userLocation.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private static FraudRiskLevel Escalate(FraudRiskLevel current, FraudRiskLevel candidate)
            => (FraudRiskLevel)Math.Max((int)current, (int)candidate);
    }
}
