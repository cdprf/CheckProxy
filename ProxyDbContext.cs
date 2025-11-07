using Microsoft.EntityFrameworkCore;

public class ProxyDbContext : DbContext
{
    public DbSet<ProxyCheck> ProxyChecks { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=proxy_checks.db");
    }
}

public class ProxyCheck
{
    public int Id { get; set; }
    public string Address { get; set; }
    public bool IsAlive { get; set; }
    public DateTime Timestamp { get; set; }
}
