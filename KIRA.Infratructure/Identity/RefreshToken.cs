namespace KIRA.Infratructure.Identity
{
    public class RefreshToken : BaseEntity
    {
        public Guid ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; } = default!;
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset ExpiresOnUtc { get; set; }
        public bool IsRevoked { get; set; }
        public DateTimeOffset? RevokedOnUtc { get; set; }
        public Guid? ReplacedByTokenId { get; set; }
    }
}
