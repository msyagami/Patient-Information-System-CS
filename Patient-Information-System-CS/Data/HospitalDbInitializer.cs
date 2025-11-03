namespace Patient_Information_System_CS.Data
{
    public static class HospitalDbInitializer
    {
        public static bool CanConnect(HospitalDbContext context) => context.Database.CanConnect();
    }
}
