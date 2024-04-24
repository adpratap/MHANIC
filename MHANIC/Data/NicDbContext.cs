using MHANIC.Models;
using Microsoft.EntityFrameworkCore;

namespace MHANIC.Data
{
    public class NicDbContext : DbContext
    {
        public NicDbContext(DbContextOptions options) : base(options) { }

        public DbSet<UserData> UsersData { get; set; }
    }
}
