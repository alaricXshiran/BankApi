using BankApi.Data;
using BankApi.Dtos;
using BankApi.Mappings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankApi.Models;
using BankApi.Dto;

namespace BankApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly BankDbContext _context;
    public AccountsController(BankDbContext context)
    {
        _context = context;
    }

    //GET: api/accounts
    [HttpGet]
    public async Task<ActionResult<List<AccountDto>>> GetAccounts()
    {
        var accounts = await _context.Accounts.ToListAsync();
        return Ok(accounts.Select(a => a.ToDto()).ToList());
    }

    //Get:api/accounts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<AccountDto>> GetAccount(int id)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account is null)
        {
            return NotFound();
        }
        return Ok(account.ToDto());
    }

    //POST:api/accounts
    [HttpPost]
    public async Task<ActionResult<AccountDto>> CreateAccount(CreateAccountDto dto)
    {
        var account = new Account
        {
            AccountNumber = await GenerateAccountNumberAsync(),
            HolderName = dto.HolderName,
            Type = dto.Type,
            Balance = dto.InitialDeposit,
            CreatedAt = DateTime.UtcNow
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account.ToDto());

    }
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

    //PUT: api/accounts/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAccount(int id, UpdateAccountDto dto)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account is null)
        {
            return NotFound();
        }
        account.HolderName = dto.holderName;
        account.Type = dto.Type;

        await _context.SaveChangesAsync();
        return NoContent();
    }


};