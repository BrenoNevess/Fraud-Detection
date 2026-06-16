namespace FraudDetection.Web.Services.Interfaces
{
    /// <summary>
    /// Motor de análise de risco de transações (valor, horário e localização).
    /// A análise é feita por uma API externa, consumida via HTTP.
    /// </summary>
    public interface IFraudDetectionService
    {
        Task<FraudAnalysisResult> AnalyzeAsync(
            decimal amount,
            DateTime when,
            string? transactionLocation,
            string? userLocation);
    }
}
