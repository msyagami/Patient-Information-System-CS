using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Patient_Information_System_CS.Configuration
{
    public static class AppConfiguration
    {
        private static readonly Lazy<IConfigurationRoot> _configuration = new(() =>
        {
            var basePath = AppContext.BaseDirectory;
            return new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        });

        public static string GetConnectionString(string name)
        {
            var connectionString = _configuration.Value.GetConnectionString(name);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Connection string '{name}' was not found in appsettings.json.");
            }

            return connectionString;
        }
    }
}
