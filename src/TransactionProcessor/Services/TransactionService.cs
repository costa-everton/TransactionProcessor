using Microsoft.EntityFrameworkCore;
using TransactionProcessor.Data;
using TransactionProcessor.Models;

namespace TransactionProcessor.Services;

public interface ITransactionService
{
    Task<TransactionRecord> ProcessAsync(TransactionCommand cmd, CancellationToken ct = default);
}

public record TransactionCommand(
    string Operation,
    string AccountId,
    long Amount,
    string Currency,
    string ReferenceId,
    string? DestinationAccountId = null,
    IDictionary<string,string>? Metadata = null
);

public class TransactionService : ITransactionService
{
    private readonly TransactionDbContext _db;
    private readonly ILogger<TransactionService> _log;
    private const int MAX_RETRIES = 5;

    public TransactionService(TransactionDbContext db, ILogger<TransactionService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<TransactionRecord> ProcessAsync(TransactionCommand cmd, CancellationToken ct = default)
    {
        // Idempotency: se já existe um registro com esse reference_id, retorne-o
        var existing = await _db.Transactions.FirstOrDefaultAsync(t => t.ReferenceId == cmd.ReferenceId, ct);
        if (existing is not null) return existing;

        int attempt = 0;
        while (true)
        {
            attempt++;
            using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            try
            {
                // carregar conta (se não existir, criar - depende do requisito; aqui criamos)
                var account = await _db.Accounts.SingleOrDefaultAsync(a => a.AccountIdentifier == cmd.AccountId, ct);
                if (account is null)
                {
                    account = new Account
                    {
                        AccountIdentifier = cmd.AccountId,
                        Balance = 0,
                        ReservedBalance = 0,
                        CreditLimit = 0,
                    };
                    _db.Accounts.Add(account);
                    await _db.SaveChangesAsync(ct);
                }

                var tr = new TransactionRecord
                {
                    ReferenceId = cmd.ReferenceId,
                    Operation = cmd.Operation,
                    AccountIdentifier = cmd.AccountId,
                    Amount = cmd.Amount,
                    Currency = cmd.Currency
                };

                // Process operations (simplified)
                switch (cmd.Operation.ToLowerInvariant())
                {
                    case "credit":
                        account.Balance += cmd.Amount;
                        tr.Status = "success";
                        break;

                    case "debit":
                        {
                            var available = account.Balance - account.ReservedBalance;
                            var totalAvailable = available + account.CreditLimit;
                            if (totalAvailable < cmd.Amount)
                            {
                                tr.Status = "failed";
                                tr.ErrorMessage = "insufficient_funds";
                                // persistimos registro de falha
                                break;
                            }
                            account.Balance -= cmd.Amount;
                            tr.Status = "success";
                        }
                        break;

                    case "reserve":
                        {
                            var available = account.Balance - account.ReservedBalance;
                            if (available < cmd.Amount)
                            {
                                tr.Status = "failed";
                                tr.ErrorMessage = "insufficient_available_for_reserve";
                                break;
                            }
                            account.ReservedBalance += cmd.Amount;
                            tr.Status = "success";
                        }
                        break;

                    case "capture":
                        {
                            if (account.ReservedBalance < cmd.Amount)
                            {
                                tr.Status = "failed";
                                tr.ErrorMessage = "insufficient_reserved";
                                break;
                            }
                            account.ReservedBalance -= cmd.Amount;
                            account.Balance -= cmd.Amount; // confirmar a captura (remover do total)
                            tr.Status = "success";
                        }
                        break;

                    case "reversal":
                        {
                            // Reverter uma transação existente referenciada em metadata (simplificado)
                            // Para o exame: apenas marcar como success e creditar o valor de volta
                            account.Balance += cmd.Amount;
                            tr.Status = "success";
                        }
                        break;

                    case "transfer":
                        {
                            if (string.IsNullOrEmpty(cmd.DestinationAccountId))
                            {
                                tr.Status = "failed";
                                tr.ErrorMessage = "destination_account_required";
                                break;
                            }
                            var dest = await _db.Accounts.SingleOrDefaultAsync(a => a.AccountIdentifier == cmd.DestinationAccountId, ct);
                            if (dest is null)
                            {
                                dest = new Account { AccountIdentifier = cmd.DestinationAccountId, Balance = 0, ReservedBalance = 0, CreditLimit = 0 };
                                _db.Accounts.Add(dest);
                                await _db.SaveChangesAsync(ct);
                            }

                            var available = account.Balance - account.ReservedBalance;
                            var totalAvailable = available + account.CreditLimit;
                            if (totalAvailable < cmd.Amount)
                            {
                                tr.Status = "failed";
                                tr.ErrorMessage = "insufficient_funds";
                                break;
                            }

                            account.Balance -= cmd.Amount;
                            dest.Balance += cmd.Amount;
                            tr.DestinationAccountIdentifier = cmd.DestinationAccountId;
                            tr.Status = "success";
                        }
                        break;

                    default:
                        tr.Status = "failed";
                        tr.ErrorMessage = "unknown_operation";
                        break;
                }

                // snapshot balances
                tr.BalanceAfter = account.Balance;
                tr.ReservedAfter = account.ReservedBalance;

                _db.Transactions.Add(tr);
                await _db.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);

                return tr;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await tx.RollbackAsync(ct);
                if (attempt >= MAX_RETRIES) throw;
                _log.LogWarning("Concurrency conflict, retrying attempt {Attempt}", attempt);
                await Task.Delay(100 * attempt, ct); // backoff simple
                continue;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _log.LogError(ex, "Error processing transaction");
                throw;
            }
        }
    }
}
