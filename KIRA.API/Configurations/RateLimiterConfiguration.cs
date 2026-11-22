namespace KIRA.API.Configurations
{
    public static class RateLimiterConfiguration
    {
        public static IServiceCollection AddAuthRateLimiting(
            this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode =
                    StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.ContentType =
                        "application/problem+json";

                    if (context.Lease.TryGetMetadata(
                        MetadataName.RetryAfter,
                        out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter =
                            Math.Ceiling(retryAfter.TotalSeconds).ToString();
                    }

                    var problem = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too Many Requests",
                        Detail = "You have exceeded the allowed request limit. Please try again later."
                    };

                    await context.HttpContext.Response.WriteAsJsonAsync(
                        problem,
                        cancellationToken);
                };

                options.AddPolicy("RegisterPolicy", context =>
                    CreateTokenBucketPolicy(
                        context,
                        tokenLimit: 5,
                        tokensPerPeriod: 2,
                        replenishmentPeriod: TimeSpan.FromMinutes(7)));

                options.AddPolicy("LoginPolicy", context =>
                    CreateTokenBucketPolicy(
                        context,
                        tokenLimit: 10,
                        tokensPerPeriod: 5,
                        replenishmentPeriod: TimeSpan.FromMinutes(1)));

                options.AddPolicy("VerifyEmailPolicy", context =>
                    CreateTokenBucketPolicy(
                        context,
                        tokenLimit: 7,
                        tokensPerPeriod: 2,
                        replenishmentPeriod: TimeSpan.FromMinutes(5)));

                options.AddPolicy("ResendOtpPolicy", context =>
                    CreateTokenBucketPolicy(
                        context,
                        tokenLimit: 5,
                        tokensPerPeriod: 2,
                        replenishmentPeriod: TimeSpan.FromMinutes(10)));

                options.AddPolicy("ForgetPasswordPolicy", context =>
                    CreateTokenBucketPolicy(
                        context,
                        tokenLimit: 5,
                        tokensPerPeriod: 2,
                        replenishmentPeriod: TimeSpan.FromMinutes(10)));

                options.AddPolicy("ValidateOtpPolicy", context =>
                    CreateTokenBucketPolicy(
                        context,
                        tokenLimit: 5,
                        tokensPerPeriod: 1,
                        replenishmentPeriod: TimeSpan.FromMinutes(5)));

                options.AddPolicy("ResetPasswordPolicy", context =>
                    CreateTokenBucketPolicy(
                        context,
                        tokenLimit: 5,
                        tokensPerPeriod: 2,
                        replenishmentPeriod: TimeSpan.FromMinutes(10)));

                options.AddPolicy("RefreshTokenPolicy", context =>
                    CreateTokenBucketPolicy(
                        context,
                        tokenLimit: 30,
                        tokensPerPeriod: 15,
                        replenishmentPeriod: TimeSpan.FromMinutes(1)));
            });

            return services;
        }

        private static RateLimitPartition<string> CreateTokenBucketPolicy(
            HttpContext context,
            int tokenLimit,
            int tokensPerPeriod,
            TimeSpan replenishmentPeriod)
        {
            var ipAddress =
                context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";

            return RateLimitPartition.GetTokenBucketLimiter(
                ipAddress,
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = tokenLimit,

                    TokensPerPeriod = tokensPerPeriod,

                    ReplenishmentPeriod = replenishmentPeriod,

                    AutoReplenishment = true,

                    QueueLimit = 0,

                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
        }
    }
}
