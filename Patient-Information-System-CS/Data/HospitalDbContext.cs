using Microsoft.EntityFrameworkCore;
using Patient_Information_System_CS.Models.Entities;

namespace Patient_Information_System_CS.Data;

public partial class HospitalDbContext : DbContext
{
    public HospitalDbContext(DbContextOptions<HospitalDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<AppointmentInvoiceLink> AppointmentInvoiceLinks { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Bill> Bills { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Doctor> Doctors { get; set; }

    public virtual DbSet<Insurance> Insurances { get; set; }

    public virtual DbSet<MedicalRecord> MedicalRecords { get; set; }

    public virtual DbSet<Nurse> Nurses { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<Person> People { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointment");

            entity.Property(e => e.AppointmentId)
                .ValueGeneratedNever()
                .HasColumnName("Appointment_ID");
            entity.Property(e => e.AppointmentIdNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Appointment_ID_Number");
            entity.Property(e => e.AppointmentPurpose)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Appointment_Purpose");
            entity.Property(e => e.AppointmentSchedule)
                .HasColumnType("datetime")
                .HasColumnName("Appointment_Schedule");
            entity.Property(e => e.AppointmentStatus).HasColumnName("Appointment_Status");
            entity.Property(e => e.AssignedDoctorId).HasColumnName("Assigned_Doctor_ID");
            entity.Property(e => e.AssignedPatientId).HasColumnName("Assigned_Patient_ID");
            entity.Property(e => e.DiagnosisText)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("Diagnosis_Text");
            entity.Property(e => e.MedicalRecordsText)
                .HasMaxLength(800)
                .IsUnicode(false)
                .HasColumnName("Medical_Records_Text");
            entity.Property(e => e.PrescriptionsText)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("Prescriptions_Text");
            entity.Property(e => e.TreatmentText)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("Treatment_Text");

            entity.HasOne(d => d.AssignedDoctor).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.AssignedDoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Appointment_Doctor");

            entity.HasOne(d => d.AssignedPatient).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.AssignedPatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Appointment_Patient");
        });

        modelBuilder.Entity<AppointmentInvoiceLink>(entity =>
        {
            entity.HasKey(e => e.LinkId).HasName("PK__Appointm__6C34E1F53CB663FC");

            entity.ToTable("Appointment_Invoice_Link");

            entity.Property(e => e.LinkId)
                .ValueGeneratedNever()
                .HasColumnName("Link_ID");
            entity.Property(e => e.AppointmentId).HasColumnName("Appointment_ID");
            entity.Property(e => e.BillId).HasColumnName("Bill_ID");

            entity.HasOne(d => d.Appointment).WithMany(p => p.AppointmentInvoiceLinks)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Link_Appointment");

            entity.HasOne(d => d.Bill).WithMany(p => p.AppointmentInvoiceLinks)
                .HasForeignKey(d => d.BillId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Link_Bill");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__Audit_Lo__2D26E7AE6B83E97C");

            entity.ToTable("Audit_Log");

            entity.Property(e => e.LogId)
                .ValueGeneratedNever()
                .HasColumnName("Log_ID");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ActionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("Action_Date");
            entity.Property(e => e.Details)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RecordId).HasColumnName("Record_ID");
            entity.Property(e => e.TableName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Table_Name");
            entity.Property(e => e.UserId).HasColumnName("User_ID");
        });

        modelBuilder.Entity<Bill>(entity =>
        {
            entity.Property(e => e.BillId)
                .ValueGeneratedNever()
                .HasColumnName("Bill_ID");
            entity.Property(e => e.Amount).HasColumnType("money");
            entity.Property(e => e.AssignedPatientId).HasColumnName("Assigned_Patient_ID");
            entity.Property(e => e.BillIdNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Bill_ID_Number");
            entity.Property(e => e.DateBilled)
                .HasColumnType("datetime")
                .HasColumnName("Date_Billed");
            entity.Property(e => e.Description)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Payment_Method");

            entity.HasOne(d => d.AssignedPatient).WithMany(p => p.Bills)
                .HasForeignKey(d => d.AssignedPatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bills_Patient");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__151675D16EBEF560");

            entity.ToTable("Department");

            entity.Property(e => e.DepartmentId)
                .ValueGeneratedNever()
                .HasColumnName("Department_ID");
            entity.Property(e => e.DepartmentName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Department_Name");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.ToTable("Doctor");

            entity.Property(e => e.DoctorId)
                .ValueGeneratedNever()
                .HasColumnName("Doctor_ID");
            entity.Property(e => e.ApprovalStatus).HasColumnName("Approval_Status");
            entity.Property(e => e.Department)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DoctorIdNumber)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Doctor_ID_Number");
            entity.Property(e => e.EmploymentDate).HasColumnName("Employment_Date");
            entity.Property(e => e.LicenseNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("License_Number");
            entity.Property(e => e.PersonId).HasColumnName("Person_ID");
            entity.Property(e => e.RegularStaff).HasColumnName("Regular_Staff");
            entity.Property(e => e.ResidencyDate).HasColumnName("Residency_Date");
            entity.Property(e => e.Salary).HasColumnType("money");
            entity.Property(e => e.Specialization)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SupervisorId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Supervisor_ID");

            entity.HasOne(d => d.Person).WithMany(p => p.Doctors)
                .HasForeignKey(d => d.PersonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Doctor_Person");
        });

        modelBuilder.Entity<Insurance>(entity =>
        {
            entity.HasKey(e => e.InsuranceId).HasName("PK__Insuranc__FFF098535045180E");

            entity.ToTable("Insurance");

            entity.Property(e => e.InsuranceId)
                .ValueGeneratedNever()
                .HasColumnName("Insurance_ID");
            entity.Property(e => e.AssignedPatientId).HasColumnName("Assigned_Patient_ID");
            entity.Property(e => e.CoverageAmount)
                .HasColumnType("money")
                .HasColumnName("Coverage_Amount");
            entity.Property(e => e.CoverageType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Coverage_Type");
            entity.Property(e => e.ExpiryDate).HasColumnName("Expiry_Date");
            entity.Property(e => e.PolicyNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Policy_Number");
            entity.Property(e => e.ProviderName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Provider_Name");

            entity.HasOne(d => d.AssignedPatient).WithMany(p => p.Insurances)
                .HasForeignKey(d => d.AssignedPatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Insurance_Patient");
        });

        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId);

            entity.ToTable("Medical_Records");

            entity.Property(e => e.RecordId)
                .ValueGeneratedNever()
                .HasColumnName("Record_ID");
            entity.Property(e => e.AssignedDoctorId).HasColumnName("Assigned_Doctor_ID");
            entity.Property(e => e.AssignedPatientId).HasColumnName("Assigned_Patient_ID");
            entity.Property(e => e.Diagnosis)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Prescriptions)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RecordDate).HasColumnName("Record_Date");
            entity.Property(e => e.RecordIdNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("Record_ID_Number");
            entity.Property(e => e.Treatment)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.AssignedDoctor).WithMany(p => p.MedicalRecords)
                .HasForeignKey(d => d.AssignedDoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Medical_Records_Doctor");

            entity.HasOne(d => d.AssignedPatient).WithMany(p => p.MedicalRecordsNavigation)
                .HasForeignKey(d => d.AssignedPatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Medical_Records_Patient");
        });

        modelBuilder.Entity<Nurse>(entity =>
        {
            entity.ToTable("Nurse");

            entity.Property(e => e.NurseId)
                .ValueGeneratedNever()
                .HasColumnName("Nurse_ID");
            entity.Property(e => e.Department)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EmploymentDate).HasColumnName("Employment_Date");
            entity.Property(e => e.LicenseNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("License_Number");
            entity.Property(e => e.NurseIdNumber)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Nurse_ID_Number");
            entity.Property(e => e.PersonId).HasColumnName("Person_ID");
            entity.Property(e => e.RegularStaff).HasColumnName("Regular_Staff");
            entity.Property(e => e.ResidencyDate).HasColumnName("Residency_Date");
            entity.Property(e => e.Salary).HasColumnType("money");
            entity.Property(e => e.Specialization)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SupervisorId)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("Supervisor_ID");

            entity.HasOne(d => d.Person).WithMany(p => p.Nurses)
                .HasForeignKey(d => d.PersonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Nurse_Person");
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patient");

            entity.Property(e => e.PatientId)
                .ValueGeneratedNever()
                .HasColumnName("Patient_ID");
            entity.Property(e => e.Allergens)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AssignedDoctorId).HasColumnName("Assigned_Doctor_ID");
            entity.Property(e => e.AssignedNurseId).HasColumnName("Assigned_Nurse_ID");
            entity.Property(e => e.BillId).HasColumnName("Bill_ID");
            entity.Property(e => e.BloodType)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("Blood_Type");
            entity.Property(e => e.CurrentMedications)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("Current_Medications");
            entity.Property(e => e.DateAdmitted)
                .HasColumnType("datetime")
                .HasColumnName("Date_Admitted");
            entity.Property(e => e.DateDischarged)
                .HasColumnType("datetime")
                .HasColumnName("Date_Discharged");
            entity.Property(e => e.MedicalHistory)
                .HasMaxLength(800)
                .IsUnicode(false)
                .HasColumnName("Medical_History");
            entity.Property(e => e.MedicalRecords).HasColumnName("Medical_Records");
            entity.Property(e => e.PatientIdNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Patient_ID_Number");
            entity.Property(e => e.PersonId).HasColumnName("Person_ID");
            entity.Property(e => e.RoomId).HasColumnName("Room_ID");
            entity.Property(e => e.Status).HasDefaultValue((byte)1);

            entity.HasOne(d => d.AssignedDoctor).WithMany(p => p.Patients)
                .HasForeignKey(d => d.AssignedDoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Patient_Doctor");

            entity.HasOne(d => d.AssignedNurse).WithMany(p => p.Patients)
                .HasForeignKey(d => d.AssignedNurseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Patient_Nurse");

            entity.HasOne(d => d.Person).WithMany(p => p.Patients)
                .HasForeignKey(d => d.PersonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Patient_Person");

            entity.HasOne(d => d.Room).WithMany(p => p.Patients)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Patient_Room");
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("Person");

            entity.Property(e => e.PersonId)
                .ValueGeneratedNever()
                .HasColumnName("Person_ID");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.ContactNumber)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("Contact_Number");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EmergencyContact)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Emergency_Contact");
            entity.Property(e => e.GivenName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Given_Name");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("Last_Name");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("MIddle_Name");
            entity.Property(e => e.Nationality)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RelationshipToEmergencyContact)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("Relationship_to_Emergency_Contact");
            entity.Property(e => e.Sex)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Suffix)
                .HasMaxLength(5)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("Room");

            entity.Property(e => e.RoomId)
                .ValueGeneratedNever()
                .HasColumnName("Room_ID");
            entity.Property(e => e.AssignedPatientId).HasColumnName("Assigned_Patient_ID");
            entity.Property(e => e.RoomNumber).HasColumnName("Room_Number");
            entity.Property(e => e.RoomType)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("Room_Type");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.Property(e => e.StaffId)
                .ValueGeneratedNever()
                .HasColumnName("Staff_ID");
            entity.Property(e => e.Department)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EmploymentDate).HasColumnName("Employment_Date");
            entity.Property(e => e.PersonId).HasColumnName("Person_ID");
            entity.Property(e => e.RegularStaff).HasColumnName("Regular_Staff");
            entity.Property(e => e.Salary).HasColumnType("money");
            entity.Property(e => e.StaffIdNumber)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("Staff_ID_Number");
            entity.Property(e => e.SupervisorId).HasColumnName("Supervisor_ID");

            entity.HasOne(d => d.Person).WithMany(p => p.Staff)
                .HasForeignKey(d => d.PersonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Staff_Person");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("User_ID");
            entity.Property(e => e.AssociatedPersonId).HasColumnName("Associated_Person_ID");
            entity.Property(e => e.Password)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.UserRole)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Patient")
                .HasColumnName("User_Role");
            entity.Property(e => e.Username)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.AssociatedPerson).WithMany(p => p.Users)
                .HasForeignKey(d => d.AssociatedPersonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Person");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
