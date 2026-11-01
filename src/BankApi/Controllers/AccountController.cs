using BankApi.Data;
using BankApi.Dtos;
using BankApi.Mappings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
};