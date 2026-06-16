using FraudDetection.Api.Models;
using FraudDetection.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FraudDetection.Api.Controllers
{
    /// <summary>
    /// Endpoint REST de análise de risco de transações.
    /// Recebe os dados em JSON e devolve o veredito (também em JSON).
    /// </summary>
    [ApiController]
    [Route("api/fraud")]
    public class FraudController : ControllerBase
    {
        private readonly FraudAnalyzer _analyzer;

        public FraudController(FraudAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        /// <summary>Analisa uma transação e retorna nível de risco, decisão e alertas.</summary>
        [HttpPost("analyze")]
        public ActionResult<FraudAnalysisResponse> Analyze([FromBody] FraudAnalysisRequest request)
        {
            FraudAnalysisOutcome outcome = _analyzer.Analyze(
                request.Amount,
                request.When,
                request.TransactionLocation,
                request.UserLocation);

            return Ok(new FraudAnalysisResponse
            {
                RiskLevel = outcome.RiskLevel.ToString(),
                Decision = outcome.Decision.ToString(),
                Alerts = outcome.Alerts,
                BlockReason = outcome.BlockReason
            });
        }
    }
}
