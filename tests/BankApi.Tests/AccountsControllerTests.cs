using BankApi.Controllers;
using BankApi.Data;
using BankApi.Dtos;
using BankApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BankApi.Tests;

// Tests for the money rules in AccountsController, using a temporary in-memory database.
// xUnit creates a NEW instance of this class for every test,
// so each test gets its own fresh database and can't affect the others.
public class AccountsControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly BankDbContext _context;
    private readonly AccountsController _controller;

    // Runs BEFORE each test: set up a fresh database and a controller that uses it.
    public AccountsControllerTests()
    {
        // An in-memory SQLite database only exists while this connection stays open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new BankDbContext(options);

        // Create all the tables from our model (this also adds the seed accounts).
        _context.Database.EnsureCreated();

        // Create the controller ourselves, handing it the test database.
        _controller = new AccountsController(_context);
    }

    // Runs AFTER each test: closing the connection deletes the in-memory database.
    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // Helper: add an account with a known balance, so each test controls its own data.
    private async Task<Account> CreateAccountAsync(decimal balance)
    {
        var account = new Account
        {
            AccountNumber = "ACC-900001",
            HolderName = "Test Customer",
            Type = AccountType.Savings,
            Balance = balance,
            CreatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return account;
    }
        [Fact]
    public async Task Deposit_IncreasesBalance_AndRecordsTransaction()
    {
        // Arrange: an account with 1,000.
        var account = await CreateAccountAsync(1000m);

        // Act: deposit 500.
        var result = await _controller.Deposit(account.Id, new TransactionRequestDto { Amount = 500m });

        // Assert: 200 OK with the new balance of 1,500.
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AccountDto>(okResult.Value);
        Assert.Equal(1500m, updated.Balance);

        // Assert: exactly one Deposit transaction was recorded.
        var transaction = Assert.Single(_context.Transactions.Where(t => t.AccountId == account.Id));
        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal(500m, transaction.Amount);
        Assert.Equal(1500m, transaction.BalanceAfter);
    }

    [Fact]
    public async Task Withdraw_DecreasesBalance_AndRecordsTransaction()
    {
        // Arrange: an account with 1,000.
        var account = await CreateAccountAsync(1000m);

        // Act: withdraw 300.
        var result = await _controller.Withdraw(account.Id, new TransactionRequestDto { Amount = 300m });

        // Assert: 200 OK with the new balance of 700.
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AccountDto>(okResult.Value);
        Assert.Equal(700m, updated.Balance);

        // Assert: exactly one Withdrawal transaction was recorded.
        var transaction = Assert.Single(_context.Transactions.Where(t => t.AccountId == account.Id));
        Assert.Equal(TransactionType.Withdrawal, transaction.Type);
        Assert.Equal(700m, transaction.BalanceAfter);
    }

    [Fact]
    public async Task Withdraw_MoreThanBalance_IsRefused_AndChangesNothing()
    {
        // Arrange: an account with only 100.
        var account = await CreateAccountAsync(100m);

        // Act: try to withdraw 500.
        var result = await _controller.Withdraw(account.Id, new TransactionRequestDto { Amount = 500m });

        // Assert: refused with 400 Bad Request.
        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, problem.StatusCode);

        // Assert: the balance is untouched, and NO transaction was recorded.
        Assert.Equal(100m, account.Balance);
        Assert.Empty(_context.Transactions.Where(t => t.AccountId == account.Id));
    }

    [Fact]
    public async Task Deposit_ToMissingAccount_ReturnsNotFound()
    {
        // Act: deposit into an account Id that doesn't exist.
        var result = await _controller.Deposit(99999, new TransactionRequestDto { Amount = 100m });

        // Assert: 404 Not Found.
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteAccount_WithMoneyLeft_IsRefused()
    {
        // Arrange: an account that still has money in it.
        var account = await CreateAccountAsync(250m);

        // Act: try to close it.
        var result = await _controller.DeleteAccount(account.Id);

        // Assert: refused with 400, and the account still exists.
        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, problem.StatusCode);
        Assert.NotNull(await _context.Accounts.FindAsync(account.Id));
    }
}