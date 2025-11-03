using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Patient_Information_System_CS.Data
{
    public sealed class HospitalDbContextFactory : IDesignTimeDbContextFactory<HospitalDbContext>
    {
        public HospitalDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var connectionString = configuration.GetConnectionString("HospitalContext");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string 'HospitalContext' was not found in appsettings.json.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<HospitalDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new HospitalDbContext(optionsBuilder.Options);
        }
    }
}
