using BankApi.Models;
using Microsoft.EntityFrameworkCore;
namespace BankApi.Data;

// The app's connection to the database: its tables and the rules for them.
public class BankDbContext : DbContext
{
    // Options (like which database to use) come from AddDbContext in Program.cs.
    public BankDbContext(DbContextOptions<BankDbContext> options)
        : base(options)
    {

    }
    // One DbSet per table.
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<User> Users => Set<User>();
        // Model-wide rules, applied to every property of a given type.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Every DateTime in every table is stored and read back as UTC.
        configurationBuilder
            .Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();
    }
    // Table rules, relationships and seed data. Migrations are generated from this.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Staff users: usernames must be unique.
                modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();

            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(u => u.PasswordHash)
                .IsRequired();
        });
        
        // Deposits and withdrawals.
        modelBuilder.Entity<Transaction>(entity =>
        {
            // Store "Deposit" / "Withdrawal" as text instead of 0 / 1.
            entity.Property(t => t.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Money: 2 decimal places.
            entity.Property(t => t.Amount)
                .HasPrecision(18, 2);

            entity.Property(t => t.BalanceAfter)
                .HasPrecision(18, 2);

            // One account has many transactions. Deleting an account deletes its transactions.
            entity.HasOne(t => t.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Account>(entity =>
        {
            //rules for the account table
            entity.HasIndex(a => a.AccountNumber).IsUnique();

            entity.Property(a => a.AccountNumber)
            .IsRequired()
            .HasMaxLength(20);
            entity.Property(a => a.HolderName)
            .IsRequired()
            .HasMaxLength(100);
            // Optional, and duplicates are allowed (one person can have many accounts).
            entity.Property(a => a.Email)
                .HasMaxLength(254);
            entity.Property(a => a.Type)
            .HasConversion<string>()
            .HasMaxLength(20);
            entity.Property(a => a.Balance)
            .HasPrecision(18, 2);



            // Sample accounts. Fixed values, so EF Core can tell when they change.
            entity.HasData(
                new Account
                {
                    Id = 1,
                    AccountNumber = "ACC-100001",
                    HolderName = "Alaric Shiran",
                    Type = AccountType.Savings,
                    Balance = 25000.00m,
                    CreatedAt = new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc)
                },
                new Account
                {
                    Id = 2,
                    AccountNumber = "ACC-100002",
                    HolderName = "champika kumara",
                    Type = AccountType.Current,
                    Balance = 150000.00m,
                    CreatedAt = new DateTime(2026, 2, 3, 10, 30, 0, DateTimeKind.Utc)
                },
                new Account
                {
                    Id = 3,
                    AccountNumber = "ACC-100003",
                    HolderName = "Anosh Mikon",
                    Type = AccountType.Savings,
                    Balance = 5250.75m,
                    CreatedAt = new DateTime(2026, 3, 20, 14, 15, 0, DateTimeKind.Utc)
                },
                 // Fourth sample account.
                new Account
                {
                    Id = 4,
                    AccountNumber = "ACC-100004",
                    HolderName = "Nishel Silva",
                    Type = AccountType.Savings,
                    Balance = 12500.00m,
                    CreatedAt = new DateTime(2026, 4, 10, 9, 30, 0, DateTimeKind.Utc)
                }

            );

        }
        );
    }
}