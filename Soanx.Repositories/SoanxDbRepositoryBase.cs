

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Soanx.Repositories {
    public class SoanxDbRepositoryBase {
        private readonly string connectionString;

        public SoanxDbRepositoryBase(string connectionString) {
            this.connectionString = connectionString;
        }

        protected SoanxDbContext CreateContext() {
            return new SoanxDbContext(connectionString);
        }
    }

    public static class DbSetExtensions {
        public static EntityEntry<T> AddIfNotExists<T>(this DbSet<T> dbSet, T entity, Expression<Func<T, bool>> predicate = null)
            where T : class, new() {

            var exists = predicate != null ? dbSet.Any(predicate) : dbSet.Any();
            return !exists ? dbSet.Add(entity) : null;
        }
    }
}
