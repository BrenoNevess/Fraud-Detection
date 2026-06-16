namespace FraudDetection.Api.Models
{
    /// <summary>Ação que o sistema deve tomar para a transação.</summary>
    public enum FraudDecision
    {
        Allow,
        RequireConfirmation,
        Block
    }
}
