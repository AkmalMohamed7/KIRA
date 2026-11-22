namespace KIRA.Infratructure.Persistence.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {

            builder.Property(x => x.FullName)
               .IsRequired()
               .HasMaxLength(100);

            builder.Property(x => x.Industry)
               .IsRequired()
               .HasMaxLength(100);

            builder.Property(x => x.CompanyName)
               .IsRequired()
               .HasMaxLength(100);

            builder.Property(x => x.BusniessDomain)
               .IsRequired()
               .HasMaxLength(100);

            builder.Property(x => x.JobTitle)
               .IsRequired()
               .HasMaxLength(100);

        }
    }
}
