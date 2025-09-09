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
    string? OriginalReferenceId = null,
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
        int attempt = 0;

        while (true)
        {
            attempt++;
            using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            try
            {
                // --- Verifica se ReferenceId já existe ---
                var existing = await _db.Transactions.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.ReferenceId == cmd.ReferenceId, ct);

                if (existing != null)
                {
                    bool isSameData =
                        existing.AccountIdentifier == cmd.AccountId &&
                        existing.Amount == cmd.Amount &&
                        existing.Currency == cmd.Currency &&
                        existing.DestinationAccountIdentifier == cmd.DestinationAccountId &&
                        existing.OriginalReferenceId == cmd.OriginalReferenceId &&
                        existing.Operation.ToString().Equals(cmd.Operation, StringComparison.OrdinalIgnoreCase);

                    if (isSameData)
                    {
                        // Idempotência válida
                        return existing;
                    }
                    else
                    {
                        // ReferenceId duplicado com dados diferentes → erro
                        return new TransactionRecord
                        {
                            ReferenceId = cmd.ReferenceId,
                            AccountIdentifier = cmd.AccountId,
                            DestinationAccountIdentifier = cmd.DestinationAccountId,
                            Amount = cmd.Amount,
                            Currency = cmd.Currency,
                            Operation = default,
                            Status = TransactionStatus.Failed,
                            ErrorCode = "error.duplicate_reference",
                            ErrorMessage = "ReferenceId already used for a different transaction",
                            BalanceAfter = 0,
                            ReservedAfter = 0
                        };
                    }
                }

                // --- Converte Operation string para enum ---
                if (!Enum.TryParse<OperationType>(cmd.Operation, true, out var operationEnum))
                {
                    var trInvalidOp = new TransactionRecord
                    {
                        ReferenceId = cmd.ReferenceId,
                        AccountIdentifier = cmd.AccountId,
                        Amount = cmd.Amount,
                        Currency = cmd.Currency,
                        Operation = default,
                        Status = TransactionStatus.Failed,
                        ErrorCode = "error.unknown_operation",
                        ErrorMessage = $"Operation '{cmd.Operation}' not recognized",
                        BalanceAfter = 0,
                        ReservedAfter = 0
                    };
                    _db.Transactions.Add(trInvalidOp);
                    await _db.SaveChangesAsync(ct);
                    return trInvalidOp;
                }

                // --- Busca conta origem ---
                var account = await _db.Accounts.SingleOrDefaultAsync(a => a.AccountIdentifier == cmd.AccountId, ct);

                if (account is null)
                {
                    var trInvalid = new TransactionRecord
                    {
                        ReferenceId = cmd.ReferenceId,
                        Operation = operationEnum,
                        AccountIdentifier = cmd.AccountId,
                        Amount = cmd.Amount,
                        Currency = cmd.Currency,
                        Status = TransactionStatus.Failed,
                        ErrorCode = "error.invalid_account",
                        ErrorMessage = "The specified account does not exist",
                        BalanceAfter = 0,
                        ReservedAfter = 0
                    };

                    _db.Transactions.Add(trInvalid);
                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    return trInvalid;
                }

                await _db.Entry(account).ReloadAsync(ct);

                var tr = new TransactionRecord
                {
                    ReferenceId = cmd.ReferenceId,
                    Operation = operationEnum,
                    AccountIdentifier = cmd.AccountId,
                    Amount = cmd.Amount,
                    Currency = cmd.Currency,
                    OriginalReferenceId = cmd.OriginalReferenceId,
                    Metadata = cmd.Metadata ?? new Dictionary<string, string>()
                };

                AccountRecord? dest = null;

                // --- Lógica por tipo de operação ---
                switch (operationEnum)
                {
                    case OperationType.Credit:
                        account.Balance += cmd.Amount;
                        tr.Status = TransactionStatus.Success;
                        break;

                    case OperationType.Debit:
                        {
                            var available = account.Balance - account.ReservedBalance;
                            var totalAvailable = available + account.CreditLimit;
                            if (totalAvailable < cmd.Amount)
                            {
                                tr.Status = TransactionStatus.Failed;
                                tr.ErrorCode = "error.insufficient_funds";
                                tr.ErrorMessage = "Insufficient funds in account";
                                break;
                            }
                            account.Balance -= cmd.Amount;
                            tr.Status = TransactionStatus.Success;
                        }
                        break;

                    case OperationType.Reserve:
                        {
                            var available = account.Balance - account.ReservedBalance;
                            if (available < cmd.Amount)
                            {
                                tr.Status = TransactionStatus.Failed;
                                tr.ErrorCode = "error.insufficient_available_for_reserve";
                                tr.ErrorMessage = "Not enough available balance to reserve";
                                break;
                            }
                            account.ReservedBalance += cmd.Amount;
                            tr.Status = TransactionStatus.Success;
                        }
                        break;

                    case OperationType.Capture:
                        {
                            if (account.ReservedBalance < cmd.Amount)
                            {
                                tr.Status = TransactionStatus.Failed;
                                tr.ErrorCode = "error.insufficient_reserved";
                                tr.ErrorMessage = "Not enough reserved balance to capture";
                                break;
                            }
                            account.ReservedBalance -= cmd.Amount;
                            account.Balance -= cmd.Amount;
                            tr.Status = TransactionStatus.Success;
                        }
                        break;

                    case OperationType.Reversal:
                        {
                            if (string.IsNullOrEmpty(cmd.OriginalReferenceId))
                            {
                                tr.Status = TransactionStatus.Failed;
                                tr.ErrorCode = "error.original_reference_required";
                                tr.ErrorMessage = "Original transaction reference is required for reversal";
                                break;
                            }

                            var original = await _db.Transactions.AsNoTracking()
                                .FirstOrDefaultAsync(t => t.ReferenceId == cmd.OriginalReferenceId, ct);

                            if (original is null || original.Status != TransactionStatus.Success)
                            {
                                tr.Status = TransactionStatus.Failed;
                                tr.ErrorCode = "error.original_transaction_not_found";
                                tr.ErrorMessage = "Original transaction not found or not successful";
                                break;
                            }

                            account.Balance += cmd.Amount;
                            tr.Status = TransactionStatus.Success;
                            tr.Metadata["reversal_of"] = cmd.OriginalReferenceId;
                        }
                        break;

                    case OperationType.Transfer:
                        {
                            if (string.IsNullOrEmpty(cmd.DestinationAccountId))
                            {
                                tr.Status = TransactionStatus.Failed;
                                tr.ErrorCode = "error.destination_account_required";
                                tr.ErrorMessage = "Destination account must be provided for transfer";
                                break;
                            }

                            dest = await _db.Accounts.SingleOrDefaultAsync(a => a.AccountIdentifier == cmd.DestinationAccountId, ct);

                            if (dest is null)
                            {
                                tr.Status = TransactionStatus.Failed;
                                tr.ErrorCode = "error.invalid_account";
                                tr.ErrorMessage = "Destination account does not exist";
                                break;
                            }

                            await _db.Entry(dest).ReloadAsync(ct);

                            var available = account.Balance - account.ReservedBalance;
                            var totalAvailable = available + account.CreditLimit;
                            if (totalAvailable < cmd.Amount)
                            {
                                tr.Status = TransactionStatus.Failed;
                                tr.ErrorCode = "error.insufficient_funds";
                                tr.ErrorMessage = "Insufficient funds for transfer";
                                break;
                            }

                            account.Balance -= cmd.Amount;
                            dest.Balance += cmd.Amount;

                            tr.DestinationAccountIdentifier = cmd.DestinationAccountId;
                            tr.Status = TransactionStatus.Success;
                        }
                        break;
                }

                // --- Snapshots ---
                tr.BalanceAfter = account.Balance;
                tr.ReservedAfter = account.ReservedBalance;

                if (dest is not null)
                {
                    tr.DestinationBalanceAfter = dest.Balance;
                    tr.DestinationReservedAfter = dest.ReservedBalance;
                }

                _db.Transactions.Add(tr);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return tr;
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync(ct);
                if (attempt >= MAX_RETRIES) throw;
                _log.LogWarning("Concurrency conflict, retrying attempt {Attempt}", attempt);
                await Task.Delay(100 * attempt, ct);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _log.LogError(ex, "Error processing transaction");

                var trFailed = new TransactionRecord
                {
                    ReferenceId = cmd.ReferenceId,
                    Operation = Enum.TryParse<OperationType>(cmd.Operation, true, out var op) ? op : default,
                    AccountIdentifier = cmd.AccountId,
                    Amount = cmd.Amount,
                    Currency = cmd.Currency,
                    Status = TransactionStatus.Failed,
                    ErrorCode = "error.exception",
                    ErrorMessage = ex.Message,
                    BalanceAfter = 0,
                    ReservedAfter = 0
                };

                _db.Transactions.Add(trFailed);
                await _db.SaveChangesAsync(ct);
                return trFailed;
            }
        }
    }
}
