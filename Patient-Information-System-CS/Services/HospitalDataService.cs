using System;
using Microsoft.EntityFrameworkCore;
using Patient_Information_System_CS.Configuration;
using Patient_Information_System_CS.Data;

namespace Patient_Information_System_CS.Services
{
    public sealed partial class HospitalDataService
    {
        private static readonly Lazy<HospitalDataService> _instance = new(() => new HospitalDataService());
        private readonly string _connectionString;

        private HospitalDataService()
        {
            _connectionString = AppConfiguration.GetConnectionString("HospitalContext");

            using var context = CreateContext();
            if (!HospitalDbInitializer.CanConnect(context))
            {
                throw new InvalidOperationException("Unable to connect to the configured hospital database. Please verify the connection string in appsettings.json.");
            }

            EnsureReferenceData();
        }

        public static HospitalDataService Instance => _instance.Value;

        public event EventHandler? AdmissionsChanged;
        public event EventHandler? AppointmentsChanged;

        private HospitalDbContext CreateContext(bool tracking = false)
        {
            var options = new DbContextOptionsBuilder<HospitalDbContext>()
                .UseSqlServer(_connectionString)
                .Options;

            var context = new HospitalDbContext(options);

            if (!tracking)
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            }

            return context;
        }

        private void RaiseAppointmentsChanged() => AppointmentsChanged?.Invoke(this, EventArgs.Empty);

        private void RaiseAdmissionsChanged() => AdmissionsChanged?.Invoke(this, EventArgs.Empty);
    }
}
