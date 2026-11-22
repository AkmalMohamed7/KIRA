namespace KIRA.Infratructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly KiraDbContext _context;
        private IDbContextTransaction? _transaction;

        private readonly Dictionary<string, object> _repositories = [];

        public UnitOfWork(KiraDbContext context)
        {
            _context = context;
        }

        public IRepository<T> Repository<T>() where T : BaseEntity
        {
            var typeName = typeof(T).Name;

            if (_repositories.TryGetValue(typeName, out object? oldRepository))
            {
                return (IRepository<T>)oldRepository;
            }

            var newRepository = new Repository<T>(_context);
            _repositories.Add(typeName, newRepository);

            return newRepository;
        }

        public async Task CommitAsync(
            CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(
            CancellationToken cancellationToken = default)
        {
            if (_transaction is not null)
            {
                throw new InvalidOperationException(
                    "A transaction is already active.");
            }

            _transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(
            CancellationToken cancellationToken = default)
        {
            if (_transaction is null)
            {
                throw new InvalidOperationException(
                    "No active transaction.");
            }

            await _transaction.CommitAsync(cancellationToken);

            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public async Task RollbackTransactionAsync(
            CancellationToken cancellationToken = default)
        {
            if (_transaction is null)
                return;

            var transaction = _transaction;
            _transaction = null;

            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }
    }
}
