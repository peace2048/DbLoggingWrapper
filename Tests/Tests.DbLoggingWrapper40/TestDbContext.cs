using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.DbLoggingWrapper
{
    public class TestDbContext : DbContext
    {
        public DbSet<Sample> Sample { get; set; }
    }

    public class Sample
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
