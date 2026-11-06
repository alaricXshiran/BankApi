using BankApi.Data;
using BankApi.Dtos;
using BankApi.Mappings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankApi.Models;
using BankApi.Dto;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;

namespace BankApi.Controllers;

[ApiController]
[Route("api/[controller]")]
// Every endpoint in this controller requires a valid JWT (logged-in staff only).
[Authorize]

public class AccountsController : ControllerBase
{
    // The database, provided by dependency injection.
    private readonly BankDbContext _context;
    public AccountsController(BankDbContext context)
    {
        _context = context;
    }

    //GET: api/accounts
    /*
    [HttpGet]
    public async Task<ActionResult<List<AccountDto>>> GetAccounts()
    {
        var accounts = await _context.Accounts.ToListAsync();
        return Ok(accounts.Select(a => a.ToDto()).ToList());
    }
    */

    //Get:api/accounts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<AccountDto>> GetAccount(int id)
    {

        var account = await _context.Accounts.FindAsync(id);

        // 404 if no account has this id.
        if (account is null)
        {
            return NotFound();
        }
        return Ok(account.ToDto());
    }

    //POST:api/accounts  (Create Account)
    [HttpPost]
    public async Task<ActionResult<AccountDto>> CreateAccount(CreateAccountDto dto)
    {
        // The bank sets the account number, balance and date; the caller can't.
        var account = new Account
        {
            AccountNumber = await GenerateAccountNumberAsync(),
            HolderName = dto.HolderName,
            Email = dto.Email?.Trim().ToLowerInvariant(),
            Type = dto.Type,
            Balance = dto.InitialDeposit,
            CreatedAt = DateTime.UtcNow
        };
        // Record the opening deposit as the account's first transaction.
        if (dto.InitialDeposit > 0)
        {
            account.Transactions.Add(new Transaction
            {
                Type = TransactionType.Deposit,
                Amount = dto.InitialDeposit,
                BalanceAfter = dto.InitialDeposit,
                CreatedAt = account.CreatedAt
            });
        }
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        // 201 Created, with a link to the new account.
        return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account.ToDto());

    }
    // Next account number: the last one plus 1 (e.g. ACC-100003 -> ACC-100004).
    private async Task<string> GenerateAccountNumberAsync()
    {
        var lastAccount = await _context.Accounts
            .OrderByDescending(a => a.Id)
            .FirstOrDefaultAsync();

        var nextNumber = lastAccount is null
            ? 100001
            : int.Parse(lastAccount.AccountNumber.Replace("ACC-", "")) + 1;

        return $"ACC-{nextNumber}";
    }

    //PUT: api/accounts/5  (Update an Account)
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAccount(int id, UpdateAccountDto dto)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account is null)
        {
            return NotFound();
        }
        // Only name, email and type can change. The balance never changes here.
        account.HolderName = dto.holderName;
        account.Email = dto.Email?.Trim().ToLowerInvariant();
        account.Type = dto.Type;

        await _context.SaveChangesAsync();
        return NoContent();
    }


    //Delete: api/account/5 (Delete an Account with no Balance)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account is null)
        {
            return NotFound();

        }
        // Never close an account that still has money in it.
        if (account.Balance > 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Account has a remaining balance.",
                detail: "Withdraw all funds before closing the account.");
        }
        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    //POST: api/accounts/5/deposit (Add money$)
    [HttpPost("{id}/deposit")]
    [EnableRateLimiting("money")]
    public async Task<ActionResult<AccountDto>> Deposit(int id, TransactionRequestDto dto)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account is null)
        {
            return NotFound();
        }
        // The new balance and the transaction record are saved together (all or nothing).
        account.Balance += dto.Amount;
        _context.Transactions.Add(new Transaction
        {
            AccountId = account.Id,
            Type = TransactionType.Deposit,
            Amount = dto.Amount,
            BalanceAfter = account.Balance,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return Ok(account.ToDto());
    }

    // POST: api/accounts/5/withdraw (take Money outside the Account)
    [HttpPost("{id}/withdraw")]
    [EnableRateLimiting("money")]
    public async Task<ActionResult<AccountDto>> Withdraw(int id, TransactionRequestDto dto)
    {
        var account = await _context.Accounts.FindAsync(id);

        if (account is null)
        {
            return NotFound();
        }

        // Can't withdraw more than the balance. Checked before anything changes.
        if (dto.Amount > account.Balance)
        {
            return Problem(
    statusCode: StatusCodes.Status400BadRequest,
    title: "Insufficient funds.",
    detail: "The withdrawal amount is greater than the account balance.");
        }

        account.Balance -= dto.Amount;

        // Saved together with the new balance (all or nothing).
        _context.Transactions.Add(new Transaction
        {
            AccountId = account.Id,
            Type = TransactionType.Withdrawal,
            Amount = dto.Amount,
            BalanceAfter = account.Balance,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(account.ToDto());
    }

    //GET:api/accounts/5/transactions
    [HttpGet("{id}/transactions")]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions(int id)
    {
        // 404 for a missing account, so it isn't mistaken for an empty history.
        var accountExists = await _context.Accounts.AnyAsync(a => a.Id == id);
        if (!accountExists)
        {
            return NotFound();
        }
        // Newest first, like a bank statement.
        var transactions = await _context.Transactions
            .Where(t => t.AccountId == id)
            .OrderByDescending(t => t.Id)
            .ToListAsync();
        return Ok(transactions.Select(t => t.ToDto()).ToList());
    }
    //Get: api/accounts?search=Alaric&type=Savings&pageSize=10
    [HttpGet]
    public async Task<ActionResult<PagedResult<AccountDto>>> GetAccounts(
        [FromQuery] AccountQueryParameters query)
    {
        var accounts = _context.Accounts.AsQueryable();

        // Search by name, account number or email
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";

            accounts = accounts.Where(a =>
                EF.Functions.Like(a.HolderName, pattern) ||
                EF.Functions.Like(a.AccountNumber, pattern) ||
                (a.Email != null && EF.Functions.Like(a.Email, pattern)));
        }

        // Filter by account type
        if (query.Type is not null)
        {
            accounts = accounts.Where(a => a.Type == query.Type);
        }

        // Count all matching accounts (before paging)
        var totalCount = await accounts.CountAsync();

        // Get just one page
        var items = await accounts
            .OrderBy(a => a.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return Ok(new PagedResult<AccountDto>
        {
            Items = items.Select(a => a.ToDto()).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        });
    }
}