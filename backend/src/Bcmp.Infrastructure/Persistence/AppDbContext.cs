using Bcmp.Domain.Jobs;
using Bcmp.Domain.Properties;
using Bcmp.Domain.AccessRequests;
using Bcmp.Domain.EmailIntake;
using Bcmp.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Bcmp.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobStatusHistory> JobStatusHistory => Set<JobStatusHistory>();
    public DbSet<AccessRequest> AccessRequests => Set<AccessRequest>();
    public DbSet<EmailIntakeMessage> EmailIntakeMessages => Set<EmailIntakeMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
