using System;
using Microsoft.AspNetCore.Identity;

namespace CoinPulse.Core.Entities;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
