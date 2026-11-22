namespace KIRA.Infratructure.Redis
{
    public sealed class RedisOtpService : IRedisOtpService
    {
        private readonly StackExchange.Redis.IDatabase _database;
        public RedisOtpService(IConnectionMultiplexer redis)
        {
            ArgumentNullException.ThrowIfNull(redis);

            _database = redis.GetDatabase();
        }


        private static string GetKey(Guid userId, string purpose)
        {
            ValidateUserId(userId);
            ValidatePurpose(purpose);

            return $"otp:{{{userId:N}}}:{purpose}";
        }

        private static void ValidateUserId(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("Invalid user ID.", nameof(userId));
        }

        private static void ValidatePurpose(string purpose)
        {
            if (string.IsNullOrWhiteSpace(purpose) ||
                purpose.Length > 50 ||
                !purpose.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
            {
                throw new ArgumentException(
                    "Invalid OTP purpose.",
                    nameof(purpose));
            }
        }

        private static void ValidateHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Hash is required.", nameof(hash));
        }

        private static void ValidateExpiration(TimeSpan expiration)
        {
            if (expiration <= TimeSpan.Zero ||
                expiration.TotalMilliseconds > long.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(expiration));
            }
        }

        private static long ToMilliseconds(TimeSpan value)
        {
            ValidateExpiration(value);

            return checked((long)value.TotalMilliseconds);
        }

        private static void ValidateOperationId(string operationId)
        {
            if (string.IsNullOrWhiteSpace(operationId) ||
                operationId.Length > 100)
            {
                throw new ArgumentException(
                    "Invalid operation ID.",
                    nameof(operationId));
            }
        }

        public async Task<bool> TryStoreOtpAsync(
            Guid userId,
            string purpose,
            string otpHash,
            TimeSpan expiration,
            int maxRequests,
            TimeSpan requestWindow,
            TimeSpan cooldown,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateHash(otpHash);
            ValidateExpiration(expiration);
            ValidateExpiration(requestWindow);
            ValidateExpiration(cooldown);

            if (maxRequests <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxRequests));

            var otpKey = GetKey(userId, purpose);

            var cooldownKey = $"{otpKey}:cooldown";
            var requestsKey = $"{otpKey}:requests";

            const string script = """
            -- Check cooldown
            if redis.call('EXISTS', KEYS[2]) == 1 then
                return 0
            end

            -- Increment request count
            local count = redis.call('INCR', KEYS[3])

            -- Set request window on first request
            if count == 1 then
                redis.call('PEXPIRE', KEYS[3], ARGV[4])
            end

            -- Check maximum requests
            if count > tonumber(ARGV[3]) then
                return 0
            end

            -- Store OTP hash
            redis.call('SET', KEYS[1], ARGV[1], 'PX', ARGV[2])

            -- Apply cooldown
            redis.call('SET', KEYS[2], '1', 'PX', ARGV[5])

            return 1
            """;

            var result = await _database.ScriptEvaluateAsync(
                script,
                new RedisKey[]
                {
                otpKey,
                cooldownKey,
                requestsKey
                },
                new RedisValue[]
                {
                otpHash,
                ToMilliseconds(expiration),
                maxRequests,
                ToMilliseconds(requestWindow),
                ToMilliseconds(cooldown)
                });

            return (int)result == 1;
        }

        public async Task<bool> VerifyAndConsumeOtpAsync(
            Guid userId,
            string purpose,
            string otpHash,
            int maxAttempts,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateHash(otpHash);

            if (maxAttempts <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts));

            var otpKey = GetKey(userId, purpose);
            var attemptsKey = $"{otpKey}:attempts";

            const string script = """
            local stored = redis.call('GET', KEYS[1])

            -- OTP does not exist or expired
            if not stored then
                return -1
            end

            local attempts = tonumber(
                redis.call('GET', KEYS[2]) or '0'
            )

            -- Maximum attempts reached
            if attempts >= tonumber(ARGV[2]) then
                return -2
            end

            -- Wrong OTP
            if stored ~= ARGV[1] then

                local count = redis.call('INCR', KEYS[2])

                if count == 1 then
                    local ttl = redis.call('PTTL', KEYS[1])

                    if ttl > 0 then
                        redis.call('PEXPIRE', KEYS[2], ttl)
                    end
                end

                return 0
            end

            -- Correct OTP: consume it
            redis.call('DEL', KEYS[1])
            redis.call('DEL', KEYS[2])

            return 1
            """;

            var result = await _database.ScriptEvaluateAsync(
                script,
                new RedisKey[]
                {
                otpKey,
                attemptsKey
                },
                new RedisValue[]
                {
                otpHash,
                maxAttempts
                });

            return (int)result == 1;
        }

        public async Task DeleteOtpAsync(
            Guid userId,
            string purpose,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var otpKey = GetKey(userId, purpose);

            await _database.KeyDeleteAsync(
                new RedisKey[]
                {
                otpKey,
                $"{otpKey}:attempts"
                });
        }


        public async Task StoreTemporarySecretAsync(
            Guid userId,
            string purpose,
            string secretHash,
            TimeSpan expiration,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateHash(secretHash);
            ValidateExpiration(expiration);

            var key = GetKey(userId, purpose);

            await _database.StringSetAsync(
                key,
                secretHash,
                expiration);
        }

        public async Task<bool> TryReserveSecretAsync(
            Guid userId,
            string purpose,
            string secretHash,
            string operationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateHash(secretHash);
            ValidateOperationId(operationId);

            var key = GetKey(userId, purpose);

            var reservationValue = $"reserved:{operationId}";

            const string script = """
            local stored = redis.call('GET', KEYS[1])

            -- Secret does not exist
            if not stored then
                return 0
            end

            -- Secret hash does not match
            if stored ~= ARGV[1] then
                return 0
            end

            -- Preserve original expiration
            local ttl = redis.call('PTTL', KEYS[1])

            if ttl <= 0 then
                return 0
            end

            -- Reserve secret
            redis.call(
                'SET',
                KEYS[1],
                ARGV[2],
                'PX',
                ttl
            )

            return 1
            """;

            var result = await _database.ScriptEvaluateAsync(
                script,
                new RedisKey[] { key },
                new RedisValue[]
                {
                secretHash,
                reservationValue
                });

            return (int)result == 1;
        }


        public async Task<bool> CompleteSecretReservationAsync(
            Guid userId,
            string purpose,
            string operationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateOperationId(operationId);

            var key = GetKey(userId, purpose);

            var reservationValue = $"reserved:{operationId}";

            const string script = """
            local stored = redis.call('GET', KEYS[1])

            -- Ensure the reservation belongs to this operation
            if stored ~= ARGV[1] then
                return 0
            end

            -- Consume the secret
            redis.call('DEL', KEYS[1])

            return 1
            """;

            var result = await _database.ScriptEvaluateAsync(
                script,
                new RedisKey[] { key },
                new RedisValue[] { reservationValue });

            return (int)result == 1;
        }

        public async Task<bool> ReleaseSecretReservationAsync(
            Guid userId,
            string purpose,
            string secretHash,
            string operationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateHash(secretHash);
            ValidateOperationId(operationId);

            var key = GetKey(userId, purpose);

            var reservationValue = $"reserved:{operationId}";

            const string script = """
            local stored = redis.call('GET', KEYS[1])

            -- Ensure this operation owns the reservation
            if stored ~= ARGV[1] then
                return 0
            end

            -- Preserve original expiration
            local ttl = redis.call('PTTL', KEYS[1])

            if ttl <= 0 then
                redis.call('DEL', KEYS[1])
                return 0
            end

            -- Restore original secret hash
            redis.call(
                'SET',
                KEYS[1],
                ARGV[2],
                'PX',
                ttl
            )

            return 1
            """;

            var result = await _database.ScriptEvaluateAsync(
                script,
                new RedisKey[] { key },
                new RedisValue[]
                {
                reservationValue,
                secretHash
                });

            return (int)result == 1;
        }
    }
}
