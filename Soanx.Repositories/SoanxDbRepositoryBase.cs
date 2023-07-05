

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

}
