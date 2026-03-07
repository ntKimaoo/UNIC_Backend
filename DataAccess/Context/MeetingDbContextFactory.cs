using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DataAccess.Context;

public class MeetingDbContextFactory
    : IDesignTimeDbContextFactory<MeetingDbContext>
{
    public MeetingDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<MeetingDbContext>();

        var connectionString =
            configuration.GetConnectionString("MeetingRoomConnection")
            ?? "Server=.;Database=MeetingRoomDb;Trusted_Connection=True;TrustServerCertificate=True";

        optionsBuilder.UseSqlServer(connectionString);

        return new MeetingDbContext(optionsBuilder.Options);
    }
}