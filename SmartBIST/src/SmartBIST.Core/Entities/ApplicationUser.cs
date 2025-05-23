using Microsoft.AspNetCore.Identity;

namespace SmartBIST.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
} 