using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soanx.Repositories {
    public class SoanxDbProvider {
		public string ConnectionString { get; set; } = null;
        private DbContextOptions<SoanxDbContext> dbOptions;

        public SoanxDbProvider() {
            dbOptions = new DbContextOptions<SoanxDbContext>();
        }

        public SoanxDbContext CreateContext() {
            if (ConnectionString != null) {
                return new SoanxDbContext(ConnectionString);
            }
            else {
                return new SoanxDbContext(dbOptions);
            }
        }
    }
}
