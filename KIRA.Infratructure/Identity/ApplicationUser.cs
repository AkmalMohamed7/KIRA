namespace KIRA.Infratructure.Identity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string BusniessDomain { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public List<string> Interests { get; set; } = null!;
        public DateOnly BirthDay { get; set; }
    }
}
