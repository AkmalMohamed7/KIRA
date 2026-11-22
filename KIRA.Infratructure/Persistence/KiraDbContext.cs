using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace KIRA.Infratructure.Persistence
{
    public  class KiraDbContext: IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public KiraDbContext(DbContextOptions<KiraDbContext>options):base(options)
        {
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
