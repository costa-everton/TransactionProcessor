using Microsoft.EntityFrameworkCore;
using TransactionProcessor.Data;
using TransactionProcessor.DTO;

namespace TransactionProcessor.Services;

public interface IAccountService
{
    Task<AccountRecord> CreateAsync(AccountCommandDto cmd);
    Task<AccountRecord> GetByIdAsync(string accountId);
    Task<List<AccountRecord>> GetAllAsync();
    Task<AccountRecord> DeleteAsync(string accountId);
}

public class AccountService : IAccountService
{
    private readonly TransactionDbContext _db;
    private readonly ILogger<AccountService> _log;

    public AccountService(TransactionDbContext db, ILogger<AccountService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<AccountRecord> CreateAsync(AccountCommandDto cmd)
    {
        try
        {
            var existing = await _db.Accounts
                .AsNoTracking()
                .SingleOrDefaultAsync(a => a.AccountIdentifier == cmd.AccountId);

            if (existing != null)
            {
                return new AccountRecord
                {
                    AccountIdentifier = cmd.AccountId,
                    Status = AccountStatus.Active,
                    ResultStatus = AccountResultStatus.Failed,
                    ErrorCode = "error.account_already_exists",
                    ErrorMessage = "Account already exists"
                };
            }

            if (cmd.InitialBalance < 0)
                return new AccountRecord
                {
                    AccountIdentifier = cmd.AccountId,
                    Status = AccountStatus.Active,
                    ResultStatus = AccountResultStatus.Failed,
                    ErrorCode = "error.invalid_initial_balance",
                    ErrorMessage = "Initial balance cannot be negative"
                };

            if (cmd.CreditLimit < 0)
                return new AccountRecord
                {
                    AccountIdentifier = cmd.AccountId,
                    Status = AccountStatus.Active,
                    ResultStatus = AccountResultStatus.Failed,
                    ErrorCode = "error.invalid_credit_limit",
                    ErrorMessage = "Credit limit cannot be negative"
                };

            var account = new AccountRecord
            {
                AccountIdentifier = cmd.AccountId,
                HolderName = cmd.AccountHolderName,
                Balance = cmd.InitialBalance,
                ReservedBalance = 0,
                CreditLimit = cmd.CreditLimit,
                Status = AccountStatus.Active,
                ResultStatus = AccountResultStatus.Success
            };

            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();

            return account;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error creating account");
            return new AccountRecord
            {
                AccountIdentifier = cmd.AccountId,
                Status = AccountStatus.Active,
                ResultStatus = AccountResultStatus.Failed,
                ErrorCode = "error.exception",
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<AccountRecord> GetByIdAsync(string accountId)
    {
        var account = await _db.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.AccountIdentifier == accountId);

        if (account == null)
            return new AccountRecord
            {
                AccountIdentifier = accountId,
                Status = AccountStatus.Active,
                ResultStatus = AccountResultStatus.Failed,
                ErrorCode = "error.account_not_found",
                ErrorMessage = "Account not found"
            };

        account.ResultStatus = AccountResultStatus.Success;
        return account;
    }

    public async Task<List<AccountRecord>> GetAllAsync()
    {
        var accounts = await _db.Accounts
            .AsNoTracking()
            .ToListAsync();

        return accounts.Select(a =>
        {
            a.ResultStatus = AccountResultStatus.Success;
            return a;
        }).ToList();
    }

    public async Task<AccountRecord> DeleteAsync(string accountId)
    {
        var account = await _db.Accounts
            .SingleOrDefaultAsync(a => a.AccountIdentifier == accountId);

        if (account == null)
            return new AccountRecord
            {
                AccountIdentifier = accountId,
                Status = AccountStatus.Active,
                ResultStatus = AccountResultStatus.Failed,
                ErrorCode = "error.account_not_found",
                ErrorMessage = "Account not found"
            };

        if (account.Balance > 0 || account.ReservedBalance > 0)
            return new AccountRecord
            {
                AccountIdentifier = accountId,
                Status = AccountStatus.Active,
                ResultStatus = AccountResultStatus.Failed,
                ErrorCode = "error.delete_not_allowed",
                ErrorMessage = "Cannot delete account with balance"
            };

        _db.Accounts.Remove(account);
        await _db.SaveChangesAsync();

        account.ResultStatus = AccountResultStatus.Success;
        return account;
    }
}
