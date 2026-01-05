using System;
using CoinPulse.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CoinPulse.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<CryptoPrice> CryptoPrices { get; set; }
    public DbSet<PortfolioTransaction> PortfolioTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CryptoPrice>()
            .Property(cp => cp.Price)
            .HasPrecision(18, 2); // Set precision for decimal values
    }
}
