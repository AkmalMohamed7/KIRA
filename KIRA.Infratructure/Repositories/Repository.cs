
namespace KIRA.Infratructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        public DbSet<T> _dbSet;
        private readonly KiraDbContext _dBContext;

        public Repository(KiraDbContext dBContext)
        {
            _dBContext = dBContext;
            _dbSet = _dBContext.Set<T>();
        }

        public async Task<T> CreateAsync(T entity, CancellationToken cancellationToken)
        {
            var entities = await _dbSet.AddAsync(entity, cancellationToken);

            return entities.Entity;
        }
        public void Update(T entity) => _dbSet.Update(entity);
        public void Remove(T entity) => _dbSet.Remove(entity);

        public async Task<IEnumerable<T>> GetAsync
           (
           Expression<Func<T, bool>>? expression = null,
           Expression<Func<T, object>>[]? include = null,
           bool Tracking = true,
           CancellationToken cancellationToken = default
           )
        {
            var entities = _dbSet.AsQueryable();

            if (expression is not null)
                entities = entities.Where(expression);

            if (include is not null)
            {
                foreach (var entity in include)
                {
                    entities = entities.Include(entity);
                }
            }

            if (!Tracking)
                entities = entities.AsNoTracking();

            return await entities.ToListAsync(cancellationToken);
        }

        public async Task<T?> GetOneAsync
           (
           Expression<Func<T, bool>>? expression = null,
           Expression<Func<T, object>>[]? include = null,
           bool Tracking = true,
           CancellationToken cancellationToken = default
           )
        {
            var query = _dbSet.AsQueryable();

            if (expression is not null)
                query = query.Where(expression);

            if (include is not null)
            {
                foreach (var row in include)
                {
                    query = query.Include(row);
                }
            }

            if (!Tracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IQueryable<T>> GetQueryable
          (
          Expression<Func<T, bool>>? expression = null,
          Expression<Func<T, object>>[]? include = null,
          bool Tracking = true,
          CancellationToken cancellationToken = default
          )
        {
            var entities = _dbSet.AsQueryable();

            if (expression is not null)
                entities = entities.Where(expression);

            if (include is not null)
            {
                foreach (var entity in include)
                {
                    entities = entities.Include(entity);
                }
            }

            if (!Tracking)
                entities = entities.AsNoTracking();

            return entities;
        }
    }
}
