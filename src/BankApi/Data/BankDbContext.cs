using BankApi.Models;
using Microsoft.EntityFrameworkCore;
namespace BankApi.Data;

public class BankDbContext : DbContext
{
    public BankDbContext(DbContextOptions<BankDbContext> options)
        : base(options)
    {

    }
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<User> Users => Set<User>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
                modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();

            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(u => u.PasswordHash)
                .IsRequired();
        });
        
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(t => t.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(t => t.Amount)
                .HasPrecision(18, 2);

            entity.Property(t => t.BalanceAfter)
                .HasPrecision(18, 2);

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
            entity.Property(a => a.Email)
                .HasMaxLength(254);
            entity.Property(a => a.Type)
            .HasConversion<string>()
            .HasMaxLength(20);
            entity.Property(a => a.Balance)
            .HasPrecision(18, 2);



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
                }

            );

        }
        );
    }
}