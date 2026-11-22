
using KIRA.Infratructure.Caching;

namespace KIRA.Infratructure
{
    public static class DependencyInjection
    {
        public static void AddInfrastructureServices(this IServiceCollection services , IConfiguration configuration )
        {
            services.AddDbContext<KiraDbContext>(options =>
            {
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"));
            });

            // Identity
            services.Configure<IdentityOptions>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            });
            services
                .AddIdentityCore<ApplicationUser>()
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<KiraDbContext>()
                .AddDefaultTokenProviders()
                .AddSignInManager<SignInManager<ApplicationUser>>();

            // Authentication

            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

            var jwtSettings = configuration.GetSection("JwtSettings");

            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var secretKey = jwtSettings["Key"];

            if (string.IsNullOrWhiteSpace(issuer))
            {
                throw new InvalidOperationException(
                    "JwtSettings:Issuer is missing.");
            }

            if (string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException(
                    "JwtSettings:Audience is missing.");
            }

            if (string.IsNullOrWhiteSpace(secretKey) ||
                Encoding.UTF8.GetByteCount(secretKey) < 32)
            {
                throw new InvalidOperationException(
                    "JwtSettings:Key must contain at least 32 bytes.");
            }


            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = issuer,

                        ValidateAudience = true,
                        ValidAudience = audience,

                        ValidateLifetime = true,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(secretKey)),

                        ClockSkew = TimeSpan.Zero
                    };
            });

            // Redis
            //services.AddSingleton<IConnectionMultiplexer>(sp =>
            //{
            //    var connectionString =
            //        configuration["Redis:ConnectionString"];

            //    if (string.IsNullOrWhiteSpace(connectionString))
            //    {
            //        throw new InvalidOperationException(
            //            "Redis:ConnectionString is missing from configuration.");
            //    }

            //    return ConnectionMultiplexer.Connect(connectionString);
            //});

            services.AddMemoryCache();

            services.AddSingleton<IOtpStorageService, InMemoryOtpStorageService>();


            //IRepository
            services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
            services.AddScoped<IRepository<RefreshToken>, Repository<RefreshToken>>();

            //Service
            services.AddScoped<KIRA.Application.Abstractions.Authentication.IAuthService,
    KIRA.Infratructure.Services.Identity.IdentityAuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ISendEmail, SendEmail.SendEmail>();
            services.AddSingleton<IOtpHasher, OtpHasher>();
            services.AddScoped<IRefreshTokenRotationService, RefreshTokenRotationService>();
            services.AddScoped<IInializar, Inializar>();
            //services.AddScoped<IRedisOtpService, RedisOtpService>();

            // Background Jobs
            services.AddTransient<SendEmailBackgroundJob>();

            services.AddScoped<
                IEmailJobScheduler,
                HangfireEmailJobScheduler>();

            var hangfireConnectionString =
                configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(hangfireConnectionString))
            {
                throw new InvalidOperationException(
                    "DefaultConnection is missing from configuration.");
            }

            services.AddHangfire(hangfireConfig =>
            {
                hangfireConfig
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(hangfireConnectionString);
            });

            services.AddHangfireServer();

            // Data Protection
            var keysPath = configuration["DataProtection:KeysPath"];

            if (string.IsNullOrWhiteSpace(keysPath))
            {
                throw new InvalidOperationException(
                    "DataProtection:KeysPath is missing from configuration.");
            }

            Directory.CreateDirectory(keysPath);

            services.AddDataProtection()
                .SetApplicationName("KIRA")
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

            services.AddSingleton<IDataProtector>(serviceProvider =>
            {
                var provider = serviceProvider
                    .GetRequiredService<IDataProtectionProvider>();

                return provider.CreateProtector(
                    "KIRA.BackgroundJobs.EmailPayload.v1");
            });
        }
       
    }
}
