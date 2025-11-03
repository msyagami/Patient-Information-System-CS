# Patient Information System (WPF)

A Windows desktop application built with WPF and .NET 8 for managing hospital admissions, billing, appointments, and medical records. QuestPDF powers PDF exports for invoices and patient records, while Entity Framework Core handles SQL Server data access.

## Prerequisites

- Windows 10/11 with desktop experience enabled (required for WPF)
- [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download)
- SQL Server 2019+ or SQL Server Express/LocalDB instance
- Optional: Visual Studio 2022 (17.8 or later) with the ".NET desktop development" workload

## Project Structure

- `Patient-Information-System-CS/` – WPF client application
- `final-pis-db.sql` – schema used by the production database
- `LICENSE.txt` – project license (GNU LGPL v2.1)

## Database Setup

1. Create an empty SQL Server database named `Group2-PIS-V2`, or choose your own name.
2. Execute `final-pis-db.sql` against that database to create all tables and constraints.
3. Update `Patient-Information-System-CS/appsettings.json` with the SQL Server connection string you want to use. Example:

   ```json
   {
     "ConnectionStrings": {
       "HospitalContext": "Server=localhost;Database=Group2-PIS-V2;Trusted_Connection=True;TrustServerCertificate=True"
     }
   }
   ```

4. Ensure the application account has rights to create, read, and update data in that database.

## Building the Application

From the repository root:

```powershell
# Restore packages and compile
 dotnet build Patient-Information-System-CS/Patient-Information-System-CS.sln
```

To run the application without Visual Studio:

```powershell
 dotnet run --project Patient-Information-System-CS/Patient-Information-System-CS/Patient-Information-System-CS.csproj
```

The first launch seeds baseline data (default rooms, departments, doctor, and nurse). Use the built-in provisioning window to create your initial administrator account.

## Testing (Optional)

If you add automated tests, run them with:

```powershell
dotnet test
```

## License

This project is distributed under the GNU Lesser General Public License v2.1. See `LICENSE.txt` for the full terms. By contributing or redistributing the software, you agree to comply with that license.
