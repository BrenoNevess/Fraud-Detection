using FraudDetection.Web.Data;
using FraudDetection.Web.Models.Entities;
using FraudDetection.Web.Models.Enums;
using FraudDetection.Web.Models.ViewModels;
using FraudDetection.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FraudDetection.Web.Services
{
    public class TransactionService : ITransactionService
    {
        private const int BlockHours = 8;

        private readonly AppDbContext _context;
        private readonly IFraudDetectionService _fraudDetection;

        public TransactionService(
            AppDbContext context,
            IFraudDetectionService fraudDetection)
        {
            _context = context;
            _fraudDetection = fraudDetection;
        }

        public async Task<FraudAnalysisResult> AnalyzeAsync(
            string senderCpf,
            string receiverCpf,
            decimal amount,
            string? location)
        {
            string sender = senderCpf.Trim();

            string? senderLocation = await _context.Users
                .Where(u => u.Cpf == sender)
                .Select(u => u.Location)
                .FirstOrDefaultAsync();

            return await _fraudDetection.AnalyzeAsync(amount, DateTime.Now, location, senderLocation);
        }

        public async Task<BlockedRecipient?> GetActiveBlockAsync(string receiverCpf)
        {
            string receiver = receiverCpf.Trim();
            DateTime now = DateTime.Now;

            return await _context.BlockedRecipients
                .Where(b => b.ReceiverCpf == receiver && b.ExpiresAt > now)
                .OrderByDescending(b => b.ExpiresAt)
                .FirstOrDefaultAsync();
        }

        public async Task<TransactionResultViewModel> CompleteAsync(
            TransactionConfirmViewModel model,
            FraudAnalysisResult analysis)
        {
            DateTime now = DateTime.Now;

            Transaction transaction = new()
            {
                Id = Guid.NewGuid(),
                SenderCpf = model.SenderCpf.Trim(),
                ReceiverCpf = model.ReceiverCpf.Trim(),
                Amount = model.Amount,
                Location = model.Location?.Trim() ?? "",
                Description = model.Description?.Trim() ?? "",
                RiskLevel = analysis.RiskLevel,
                IsBlocked = false,
                SignedBy = string.IsNullOrWhiteSpace(model.SignatureName)
                    ? null
                    : model.SignatureName.Trim(),
                CreatedAt = now
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return new TransactionResultViewModel
            {
                RiskLevel = analysis.RiskLevel,
                Alerts = analysis.Alerts,
                Amount = transaction.Amount,
                CreatedAt = transaction.CreatedAt
            };
        }

        public async Task<BlockedTransactionViewModel> BlockAsync(
            TransactionConfirmViewModel model,
            FraudAnalysisResult analysis)
        {
            DateTime now = DateTime.Now;
            DateTime expiresAt = now.AddHours(BlockHours);
            string sender = model.SenderCpf.Trim();
            string receiver = model.ReceiverCpf.Trim();
            string reason = analysis.BlockReason ?? "Transação de alto risco.";

            // Registra a transação bloqueada (auditoria/histórico).
            _context.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                SenderCpf = sender,
                ReceiverCpf = receiver,
                Amount = model.Amount,
                Location = model.Location?.Trim() ?? "",
                Description = model.Description?.Trim() ?? "",
                RiskLevel = analysis.RiskLevel,
                IsBlocked = true,
                CreatedAt = now
            });

            // Insere ou renova o bloqueio do destinatário (8h).
            BlockedRecipient? existing = await _context.BlockedRecipients
                .FirstOrDefaultAsync(b => b.ReceiverCpf == receiver && b.ExpiresAt > now);

            if (existing is not null)
            {
                existing.SenderCpf = sender;
                existing.Reason = reason;
                existing.BlockedAt = now;
                existing.ExpiresAt = expiresAt;
            }
            else
            {
                _context.BlockedRecipients.Add(new BlockedRecipient
                {
                    Id = Guid.NewGuid(),
                    ReceiverCpf = receiver,
                    SenderCpf = sender,
                    Reason = reason,
                    BlockedAt = now,
                    ExpiresAt = expiresAt
                });
            }

            await _context.SaveChangesAsync();

            return new BlockedTransactionViewModel
            {
                ReceiverCpf = receiver,
                Amount = model.Amount,
                BlockReason = reason,
                BlockedUntil = expiresAt,
                Alerts = analysis.Alerts
            };
        }

        public async Task<IReadOnlyList<Transaction>> GetByUserAsync(string cpf)
        {
            cpf = cpf.Trim();

            return await _context.Transactions
                .Where(t => t.SenderCpf == cpf || t.ReceiverCpf == cpf)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Transaction>> GetFraudulentAsync()
        {
            return await _context.Transactions
                .Where(t => t.RiskLevel != FraudRiskLevel.Safe || t.IsBlocked)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<BlockedRecipient>> GetActiveBlocksAsync()
        {
            DateTime now = DateTime.Now;

            return await _context.BlockedRecipients
                .Where(b => b.ExpiresAt > now)
                .OrderByDescending(b => b.BlockedAt)
                .ToListAsync();
        }

        public async Task RemoveBlockAsync(Guid id)
        {
            BlockedRecipient? block = await _context.BlockedRecipients.FindAsync(id);

            if (block is not null)
            {
                _context.BlockedRecipients.Remove(block);
                await _context.SaveChangesAsync();
            }
        }
    }
}
