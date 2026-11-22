namespace KIRA.Infratructure.DBInializar
{
    public class Inializar : IInializar
    {
        private readonly KiraDbContext _dbContext;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public Inializar(
            KiraDbContext dbContext,
            RoleManager<IdentityRole<Guid>> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<Result> Inializ()
        {
            if ((await _dbContext.Database.GetPendingMigrationsAsync()).Any())
            {
                await _dbContext.Database.MigrateAsync();
            }

            var roles = new[]
             {
                Roles.SuperAdmin,
                Roles.Admin,
                Roles.Client
            };

            foreach (var role in roles)
            {
                var roleResult = await CreateRoleAsync(role);

                if (!roleResult.IsSuccess)
                    return roleResult;
            }

            var email = "superAdmin@gmail.com";

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                var newUser = new ApplicationUser
                {
                    FullName = "SuperAdmin",
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true,
                    BusniessDomain = "Admin",
                    PhoneNumber = "01153005756",
                    PhoneNumberConfirmed = true,
                    JobTitle = "SuperAdmin",
                    CompanyName = "Kira",
                    Interests = new List<string> { "SuperAdmin" },
                    BirthDay = DateOnly.FromDateTime(DateTime.Now)
                };

                var createResult = await _userManager.CreateAsync(
                    newUser,
                    "Admin123#"
                );

                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ",
                        createResult.Errors.Select(e => e.Description));

                    return Result.Failure(new Shared.Result.Error(
                        "CanNotCreateUser",
                        errors,
                        ErrorType.Failure
                    ));
                }

                var addRoleResult = await _userManager.AddToRoleAsync(
                    newUser,
                    Roles.SuperAdmin
                );

                if (!addRoleResult.Succeeded)
                {
                    var errors = string.Join(", ",
                        addRoleResult.Errors.Select(e => e.Description));

                    return Result.Failure(new Shared.Result.Error(
                        "CanNotAddToRole",
                        errors,
                        ErrorType.Failure
                    ));
                }
            }

            return Result.Success();
        }

        private async Task<Result> CreateRoleAsync(string name)
        {
            if (await _roleManager.RoleExistsAsync(name))
                return Result.Success();

            var result = await _roleManager.CreateAsync(
                new IdentityRole<Guid>(name)
            );

            if (!result.Succeeded)
            {
                var errors = string.Join(", ",
                    result.Errors.Select(e => e.Description));

                return Result.Failure(new Shared.Result.Error(
                    "CanNotCreateRole",
                    errors,
                    ErrorType.Failure
                ));
            }

            return Result.Success();
        }
    }
}
