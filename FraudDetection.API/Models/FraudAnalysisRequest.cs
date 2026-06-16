using System.ComponentModel.DataAnnotations;

namespace FraudDetection.Api.Models
{
    /// <summary>
    /// Dados enviados pelo cliente (app MVC) para análise de risco.
    /// </summary>
    public class FraudAnalysisRequest
    {
        /// <summary>Valor da transação.</summary>
        [Range(0.01, 999_999_999)]
        public decimal Amount { get; set; }

        /// <summary>Data/hora em que a transação ocorre.</summary>
        public DateTime When { get; set; }

        /// <summary>Localização de onde a transação parte.</summary>
        public string? TransactionLocation { get; set; }

        /// <summary>Localização cadastrada do remetente.</summary>
        public string? UserLocation { get; set; }
    }
}
