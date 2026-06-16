using FraudDetection.Api.Models;
using FraudDetection.Api.Services;

namespace TravaPix.Tests
{
    // Testa a lógica de análise de fraude onde ela vive: na API (FraudAnalyzer).
    public class FraudDetectionServiceTests
    {
        private readonly FraudAnalyzer _analyzer = new();

        // Horário comercial (não atípico) para cenários controlados.
        private static DateTime AtHour(int hour) => new(2026, 6, 4, hour, 0, 0);

        [Fact]
        public void Analyze_SmallAmountDuringDay_ReturnsSafeAndAllow()
        {
            FraudAnalysisOutcome result = _analyzer.Analyze(
                amount: 100m,
                when: AtHour(14),
                transactionLocation: "Sao Paulo/SP",
                userLocation: "Sao Paulo/SP");

            Assert.Equal(FraudRiskLevel.Safe, result.RiskLevel);
            Assert.Equal(FraudDecision.Allow, result.Decision);
            Assert.Null(result.BlockReason);
            Assert.Empty(result.Alerts);
        }

        [Fact]
        public void Analyze_AmountAboveNormalDuringDay_ReturnsSuspiciousAndRequiresConfirmation()
        {
            FraudAnalysisOutcome result = _analyzer.Analyze(
                amount: 1500m,
                when: AtHour(14),
                transactionLocation: "Sao Paulo/SP",
                userLocation: "Sao Paulo/SP");

            Assert.Equal(FraudRiskLevel.Suspicious, result.RiskLevel);
            Assert.Equal(FraudDecision.RequireConfirmation, result.Decision);
            Assert.Contains("Valor acima do normal.", result.Alerts);
        }

        [Theory]
        [InlineData(5000, 14, "Sao Paulo/SP", "Sao Paulo/SP")]      // valor extremamente elevado
        [InlineData(1500, 2, "Rio de Janeiro/RJ", "Sao Paulo/SP")]  // acima do normal + horário atípico + localização diferente
        public void Analyze_BlockScenarios_ReturnBlock(
            decimal amount,
            int hour,
            string transactionLocation,
            string userLocation)
        {
            FraudAnalysisOutcome result = _analyzer.Analyze(
                amount,
                AtHour(hour),
                transactionLocation,
                userLocation);

            Assert.Equal(FraudDecision.Block, result.Decision);
            Assert.Equal(FraudRiskLevel.HighRisk, result.RiskLevel);
            Assert.False(string.IsNullOrWhiteSpace(result.BlockReason));
        }
    }
}
