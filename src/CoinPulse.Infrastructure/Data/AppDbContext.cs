using System;
using CoinPulse.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoinPulse.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<CryptoPrice> CryptoPrices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CryptoPrice>()
            .Property(cp => cp.Price)
            .HasPrecision(18, 2); // Set precision for decimal values
    }
}
