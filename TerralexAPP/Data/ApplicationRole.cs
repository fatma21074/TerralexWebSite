using Microsoft.AspNetCore.Identity;

namespace TerralexAPP.Data
{
    public class ApplicationRole : IdentityRole<int>
    {
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? CreatedBy { get; set; }
    }
}
