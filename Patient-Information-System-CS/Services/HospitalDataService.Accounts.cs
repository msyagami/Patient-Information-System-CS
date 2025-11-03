using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Patient_Information_System_CS.Data;
using Patient_Information_System_CS.Models;
using EntityBill = Patient_Information_System_CS.Models.Entities.Bill;
using EntityDoctor = Patient_Information_System_CS.Models.Entities.Doctor;
using EntityPatient = Patient_Information_System_CS.Models.Entities.Patient;
using EntityPerson = Patient_Information_System_CS.Models.Entities.Person;
using EntityStaff = Patient_Information_System_CS.Models.Entities.Staff;
using EntityUser = Patient_Information_System_CS.Models.Entities.User;

namespace Patient_Information_System_CS.Services
{
    public sealed partial class HospitalDataService
    {
        public UserAccount? GetAccountByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            using var context = CreateContext();

            var userEntity = BuildUserQuery(context)
                .SingleOrDefault(u => u.Username == username);

            if (userEntity is null)
            {
                return null;
            }

            var billsLookup = CreateBillLookup(context);
            return MapUserAccount(userEntity, billsLookup);
        }

        public bool RequiresAdminProvisioning()
        {
            using var context = CreateContext();
            return RequiresAdminProvisioning(context);
        }

        public UserAccount ProvisionFirstAdmin(AdminProvisioningRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.GivenName) || string.IsNullOrWhiteSpace(request.LastName))
            {
                throw new ArgumentException("Admin name must include at least a given and last name.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Password is required for the initial administrator.");
            }

            using var context = CreateContext(tracking: true);

            if (!RequiresAdminProvisioning(context))
            {
                throw new InvalidOperationException("An administrator account already exists.");
            }

            var username = request.Username?.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                username = GenerateUniqueUsername(context, $"{request.GivenName}{request.LastName}", "admin");
            }
            else if (context.Users.Any(u => u.Username == username))
            {
                throw new InvalidOperationException("The chosen username is already in use.");
            }

            var email = string.IsNullOrWhiteSpace(request.Email)
                ? GenerateEmailAddress($"{request.GivenName} {request.LastName}")
                : request.Email.Trim();

            var personId = NextPersonId(context);
            var userId = NextUserId(context);

            var sexCode = NormalizeSexCode(request.Sex);

            var person = new EntityPerson
            {
                PersonId = personId,
                GivenName = request.GivenName.Trim(),
                LastName = request.LastName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(),
                Suffix = string.IsNullOrWhiteSpace(request.Suffix) ? null : request.Suffix.Trim(),
                Birthdate = DateOnly.FromDateTime(request.BirthDate.Date),
                Sex = sexCode,
                ContactNumber = ParseContactNumber(request.ContactNumber),
                Address = string.IsNullOrWhiteSpace(request.Address) ? "Not Provided" : request.Address.Trim(),
                EmergencyContact = string.IsNullOrWhiteSpace(request.EmergencyContact) ? "Not Provided" : request.EmergencyContact.Trim(),
                RelationshipToEmergencyContact = string.IsNullOrWhiteSpace(request.RelationshipToEmergencyContact)
                    ? "Unknown"
                    : request.RelationshipToEmergencyContact.Trim(),
                Email = email,
                Nationality = string.IsNullOrWhiteSpace(request.Nationality) ? "Unknown" : request.Nationality.Trim()
            };

            var user = new EntityUser
            {
                UserId = userId,
                Username = username,
                Password = EncodePassword(request.Password.Trim()),
                AssociatedPersonId = personId,
                UserRole = "admin"
            };

            context.People.Add(person);
            context.Users.Add(user);
            context.SaveChanges();

            var createdUser = BuildUserQuery(context).Single(u => u.UserId == userId);
            var account = MapUserAccount(createdUser, CreateBillLookup(context));

            return account;
        }

        public IEnumerable<UserAccount> GetAllAccounts() => LoadAccounts();

        public IEnumerable<UserAccount> GetAllDoctors() =>
            LoadAccounts().Where(account => account.Role == UserRole.Doctor);

        public IEnumerable<UserAccount> GetAllPatients() =>
            LoadAccounts().Where(account => account.Role == UserRole.Patient);

        public IEnumerable<UserAccount> GetPatientsForDoctor(int doctorId) =>
            LoadAccounts().Where(account => account.Role == UserRole.Patient &&
                                            account.PatientProfile is not null &&
                                            account.PatientProfile.AssignedDoctorId == doctorId);

        public IEnumerable<UserAccount> GetApprovedStaff() =>
            LoadAccounts().Where(account => (account.Role == UserRole.Staff || account.Role == UserRole.Admin) &&
                                            ((account.AdminProfile?.IsApproved ?? false) ||
                                             (account.StaffProfile?.IsApproved ?? false)));

        public IEnumerable<UserAccount> GetPendingStaff() =>
            LoadAccounts().Where(account => (account.Role == UserRole.Staff || account.Role == UserRole.Admin) &&
                                            !((account.AdminProfile?.IsApproved ?? false) ||
                                              (account.StaffProfile?.IsApproved ?? false)));

        public void ApproveStaff(UserAccount account)
        {
            using var context = CreateContext(tracking: true);

            var userEntity = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Staff)
                .SingleOrDefault(u => u.UserId == account.UserId);

            var staffEntity = userEntity?.AssociatedPerson?.Staff.FirstOrDefault();
            if (staffEntity is null)
            {
                return;
            }

            staffEntity.RegularStaff = true;
            context.SaveChanges();
            RaiseAdmissionsChanged();
        }

        public void RejectStaff(UserAccount account)
        {
            using var context = CreateContext(tracking: true);

            var userEntity = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Staff)
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Users)
                .SingleOrDefault(u => u.UserId == account.UserId);

            if (userEntity is null)
            {
                return;
            }

            var person = userEntity.AssociatedPerson;
            var staffEntity = person.Staff.FirstOrDefault();

            if (staffEntity is not null)
            {
                context.Staff.Remove(staffEntity);
            }

            context.Users.Remove(userEntity);

            if (person.Users.Count <= 1 &&
                !person.Doctors.Any() &&
                !person.Patients.Any())
            {
                context.People.Remove(person);
            }

            context.SaveChanges();
            RaiseAdmissionsChanged();
        }

        public IEnumerable<UserAccount> GetActiveDoctors() =>
            LoadAccounts().Where(account => account.Role == UserRole.Doctor &&
                                            account.DoctorProfile is { Status: DoctorStatus.Available });

        public IEnumerable<UserAccount> GetPendingDoctors() =>
            LoadAccounts().Where(account => account.Role == UserRole.Doctor &&
                                            account.DoctorProfile is { Status: DoctorStatus.OnHold });

        public IEnumerable<UserAccount> GetUnavailableDoctors() =>
            LoadAccounts().Where(account => account.Role == UserRole.Doctor &&
                                            account.DoctorProfile is { Status: DoctorStatus.NotAvailable });

        public void ApproveDoctor(UserAccount doctor)
        {
            using var context = CreateContext(tracking: true);

            var userEntity = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Doctors)
                .SingleOrDefault(u => u.UserId == doctor.UserId);

            var doctorEntity = userEntity?.AssociatedPerson?.Doctors.FirstOrDefault();
            if (doctorEntity is null)
            {
                return;
            }

            doctorEntity.ApprovalStatus = MapDoctorStatusToByte(DoctorStatus.Available);
            doctorEntity.RegularStaff = true;
            context.SaveChanges();
            RaiseAdmissionsChanged();
        }

        public void RejectDoctor(UserAccount doctor)
        {
            using var context = CreateContext(tracking: true);

            var userEntity = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Doctors)
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Users)
                .SingleOrDefault(u => u.UserId == doctor.UserId);

            if (userEntity is null)
            {
                return;
            }

            var person = userEntity.AssociatedPerson;

            foreach (var doctorEntry in person.Doctors.ToList())
            {
                context.Doctors.Remove(doctorEntry);
            }

            context.Users.Remove(userEntity);

            if (person.Users.Count <= 1 &&
                !person.Staff.Any() &&
                !person.Patients.Any())
            {
                context.People.Remove(person);
            }

            context.SaveChanges();
            RaiseAdmissionsChanged();
        }

        public void ToggleDoctorAvailability(UserAccount doctor)
        {
            using var context = CreateContext(tracking: true);

            var userEntity = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Doctors)
                .SingleOrDefault(u => u.UserId == doctor.UserId);

            var doctorEntity = userEntity?.AssociatedPerson?.Doctors.FirstOrDefault();
            if (doctorEntity is null)
            {
                return;
            }

            var current = MapDoctorStatus(doctorEntity.ApprovalStatus);
            var next = current == DoctorStatus.Available ? DoctorStatus.NotAvailable : DoctorStatus.Available;
            doctorEntity.ApprovalStatus = MapDoctorStatusToByte(next);
            context.SaveChanges();
            RaiseAdmissionsChanged();
        }

        public void UpdateDoctorStatus(UserAccount doctor, DoctorStatus status)
        {
            using var context = CreateContext(tracking: true);

            var userEntity = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Doctors)
                .SingleOrDefault(u => u.UserId == doctor.UserId);

            var doctorEntity = userEntity?.AssociatedPerson?.Doctors.FirstOrDefault();
            if (doctorEntity is null)
            {
                return;
            }

            doctorEntity.ApprovalStatus = MapDoctorStatusToByte(status);
            context.SaveChanges();
            RaiseAdmissionsChanged();
        }

        public IEnumerable<UserAccount> GetApprovedPatients() =>
            LoadAccounts().Where(account => account.Role == UserRole.Patient &&
                                            (account.PatientProfile?.IsApproved ?? false));

        public IEnumerable<UserAccount> GetPendingPatients() =>
            LoadAccounts().Where(account => account.Role == UserRole.Patient &&
                                            !(account.PatientProfile?.IsApproved ?? false));

        public IEnumerable<UserAccount> GetPatientsWithPendingBills() =>
            LoadAccounts().Where(account => account.Role == UserRole.Patient &&
                                            (account.PatientProfile?.HasUnpaidBills ?? false));

        public void ApprovePatient(UserAccount patient)
        {
            using var context = CreateContext(tracking: true);

            var patientEntity = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                .SingleOrDefault(u => u.UserId == patient.UserId)?
                .AssociatedPerson?.Patients.FirstOrDefault();

            if (patientEntity is null)
            {
                return;
            }

            patientEntity.Status = 1;
            context.SaveChanges();
            RaiseAdmissionsChanged();
        }

        public void RejectPatient(UserAccount patient)
        {
            using var context = CreateContext(tracking: true);

            var userEntity = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Users)
                .SingleOrDefault(u => u.UserId == patient.UserId);

            var patientEntity = userEntity?.AssociatedPerson?.Patients.FirstOrDefault();
            if (patientEntity is null)
            {
                return;
            }

            context.Patients.Remove(patientEntity);

            if (userEntity is not null)
            {
                context.Users.Remove(userEntity);
            }

            var person = patientEntity.Person;
            if (person.Users.Count <= 1 &&
                !person.Staff.Any() &&
                !person.Doctors.Any())
            {
                context.People.Remove(person);
            }

            context.SaveChanges();
            RaiseAdmissionsChanged();
        }

        public IEnumerable<UserAccount> GetCurrentAdmissions() =>
            LoadAccounts().Where(account => account.Role == UserRole.Patient &&
                                            account.PatientProfile is { IsCurrentlyAdmitted: true });

        public IEnumerable<UserAccount> GetDeactivatedPatients() =>
            LoadAccounts().Where(account => account.Role == UserRole.Patient &&
                                            (!account.IsActive || (account.PatientProfile?.HasUnpaidBills ?? false)));

        public void DischargePatient(UserAccount patient)
        {
            using var context = CreateContext(tracking: true);

            var patientEntity = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                .SingleOrDefault(u => u.UserId == patient.UserId)?
                .AssociatedPerson?.Patients.FirstOrDefault();

            if (patientEntity is null)
            {
                return;
            }

            patientEntity.DateDischarged = DateTime.Now;
            patientEntity.Status = 0;
            context.SaveChanges();
            RaiseAdmissionsChanged();
        }

        public void ReactivatePatient(UserAccount patient)
        {
            using var context = CreateContext(tracking: true);

            var patientEntity = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                .SingleOrDefault(u => u.UserId == patient.UserId)?
                .AssociatedPerson?.Patients.FirstOrDefault();

            if (patientEntity is null)
            {
                return;
            }

            patientEntity.Status = 1;
            patientEntity.DateDischarged = null;

            MarkInvoicesPaidForPatient(context, patientEntity.PatientId);

            context.SaveChanges();
            RaiseAdmissionsChanged();
        }

        public IEnumerable<UserAccount> GetSchedulablePatients() => GetApprovedPatients();

        public UserAccount AdmitNewPatient(string fullName,
                           DateTime dateOfBirth,
                           string contactNumber,
                           string address,
                           string emergencyContact,
                           string insuranceProvider,
                           UserAccount? doctor,
                           string roomAssignment,
                           DateTime? admitDateOverride = null,
                           string sex = DefaultSexCode,
                           string emergencyRelationship = "Unknown",
                           string nationality = "Unknown")
        {
            var email = GenerateEmailAddress(fullName);
            var doctorId = doctor?.UserId;

            return CreatePatientAccount(fullName,
                                        email,
                                        contactNumber,
                                        address,
                                        dateOfBirth,
                                        approve: true,
                                        currentlyAdmitted: true,
                                        doctorId,
                                        insuranceProvider,
                                        emergencyContact,
                                        roomAssignment,
                                        sex,
                                        emergencyRelationship,
                                        nationality,
                                        admitDateOverride: admitDateOverride);
        }

        public UserAccount? GetDoctorById(int? doctorUserId)
        {
            if (doctorUserId is null)
            {
                return null;
            }

            return GetAllDoctors().FirstOrDefault(account => account.UserId == doctorUserId);
        }

        public UserAccount CreateStaffAccount(string fullName,
                                              string email,
                                              string contactNumber,
                                              bool approve,
                                              DateTime birthDate,
                                              string sex,
                                              string address,
                                              string emergencyContact,
                                              string emergencyRelationship,
                                              string nationality)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException("Full name is required", nameof(fullName));
            }

            using var context = CreateContext(tracking: true);

            var personId = NextPersonId(context);
            var staffId = NextStaffId(context);
            var userId = NextUserId(context);

            var username = GenerateUniqueUsername(context, fullName, "staff");
            var emailAddress = string.IsNullOrWhiteSpace(email) ? GenerateEmailAddress(fullName) : email.Trim();
            var contactValue = ParseContactNumber(contactNumber);
            var nameParts = ParseName(fullName);
            var normalizedSex = NormalizeSexCode(sex);

            var person = new EntityPerson
            {
                PersonId = personId,
                GivenName = nameParts.GivenName,
                LastName = nameParts.LastName,
                MiddleName = nameParts.MiddleName,
                Birthdate = DateOnly.FromDateTime(birthDate.Date),
                Sex = normalizedSex,
                ContactNumber = contactValue,
                Address = string.IsNullOrWhiteSpace(address) ? "Not Provided" : address.Trim(),
                EmergencyContact = string.IsNullOrWhiteSpace(emergencyContact) ? "Not Provided" : emergencyContact.Trim(),
                RelationshipToEmergencyContact = string.IsNullOrWhiteSpace(emergencyRelationship) ? "Unknown" : emergencyRelationship.Trim(),
                Email = emailAddress,
                Nationality = string.IsNullOrWhiteSpace(nationality) ? "Unknown" : nationality.Trim()
            };

            var staff = new EntityStaff
            {
                StaffId = staffId,
                StaffIdNumber = $"STF-{staffId:D5}",
                Department = "Administration",
                EmploymentDate = DateOnly.FromDateTime(DateTime.Today),
                RegularStaff = approve,
                SupervisorId = null,
                Salary = 0m,
                PersonId = personId
            };

            var user = new EntityUser
            {
                UserId = userId,
                Username = username,
                Password = EncodePassword("changeme"),
                AssociatedPersonId = personId,
                UserRole = "staff"
            };

            context.People.Add(person);
            context.Staff.Add(staff);
            context.Users.Add(user);
            context.SaveChanges();

            var createdUser = BuildUserQuery(context).Single(u => u.UserId == userId);
            var account = MapUserAccount(createdUser, CreateBillLookup(context));

            RaiseAdmissionsChanged();
            return account;
        }

        public UserAccount CreateDoctorAccount(string fullName,
                                               string email,
                                               string contactNumber,
                                               string department,
                                               string licenseNumber,
                                               string address,
                                               DoctorStatus status,
                                               DateTime birthDate,
                                               string sex,
                                               string emergencyContact,
                                               string emergencyRelationship,
                                               string nationality)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException("Full name is required", nameof(fullName));
            }

            using var context = CreateContext(tracking: true);

            var personId = NextPersonId(context);
            var doctorId = NextDoctorId(context);
            var userId = NextUserId(context);

            var username = GenerateUniqueUsername(context, fullName, "doctor");
            var emailAddress = string.IsNullOrWhiteSpace(email) ? GenerateEmailAddress(fullName) : email.Trim();
            var contactValue = ParseContactNumber(contactNumber);
            var nameParts = ParseName(fullName);
            var normalizedSex = NormalizeSexCode(sex);

            var person = new EntityPerson
            {
                PersonId = personId,
                GivenName = nameParts.GivenName,
                LastName = nameParts.LastName,
                MiddleName = nameParts.MiddleName,
                Birthdate = DateOnly.FromDateTime(birthDate.Date),
                Sex = normalizedSex,
                ContactNumber = contactValue,
                Address = string.IsNullOrWhiteSpace(address) ? "Not Provided" : address.Trim(),
                EmergencyContact = string.IsNullOrWhiteSpace(emergencyContact) ? "Not Provided" : emergencyContact.Trim(),
                RelationshipToEmergencyContact = string.IsNullOrWhiteSpace(emergencyRelationship) ? "Unknown" : emergencyRelationship.Trim(),
                Email = emailAddress,
                Nationality = string.IsNullOrWhiteSpace(nationality) ? "Unknown" : nationality.Trim()
            };

            var doctorEntity = new EntityDoctor
            {
                DoctorId = doctorId,
                DoctorIdNumber = $"DOC-{doctorId:D5}",
                LicenseNumber = string.IsNullOrWhiteSpace(licenseNumber) ? $"LIC-{doctorId:D5}" : licenseNumber,
                Department = string.IsNullOrWhiteSpace(department) ? "General Medicine" : department,
                Specialization = string.IsNullOrWhiteSpace(department) ? "General" : department,
                EmploymentDate = DateOnly.FromDateTime(DateTime.Today),
                RegularStaff = status == DoctorStatus.Available,
                ResidencyDate = null,
                SupervisorId = null,
                Salary = 0m,
                PersonId = personId,
                ApprovalStatus = MapDoctorStatusToByte(status)
            };

            var user = new EntityUser
            {
                UserId = userId,
                Username = username,
                Password = EncodePassword("doctor123"),
                AssociatedPersonId = personId,
                UserRole = "doctor"
            };

            context.People.Add(person);
            context.Doctors.Add(doctorEntity);
            context.Users.Add(user);
            context.SaveChanges();

            var createdUser = BuildUserQuery(context).Single(u => u.UserId == userId);
            var account = MapUserAccount(createdUser, CreateBillLookup(context));

            RaiseAdmissionsChanged();
            return account;
        }

        public UserAccount CreatePatientAccount(string fullName,
                            string email,
                            string contactNumber,
                            string address,
                            DateTime dateOfBirth,
                            bool approve,
                            bool currentlyAdmitted,
                            int? assignedDoctorUserId,
                            string insuranceProvider,
                            string emergencyContact,
                            string roomAssignment,
                            string sex = DefaultSexCode,
                            string emergencyRelationship = "Unknown",
                            string nationality = "Unknown",
                            DateTime? admitDateOverride = null)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException("Full name is required", nameof(fullName));
            }

            using var context = CreateContext(tracking: true);

            var doctorId = ResolveDoctorId(context, assignedDoctorUserId) ?? ResolveFallbackDoctor(context);
            var nurseId = ResolveFallbackNurse(context);
            if (nurseId is null)
            {
                throw new InvalidOperationException("No nurse records are available. Please seed the Nurse table before admitting patients.");
            }

            var room = ResolveRoom(context, roomAssignment);

            var personId = NextPersonId(context);
            var patientId = NextPatientId(context);
            var userId = NextUserId(context);
            var billId = NextBillId(context);

            var patientNumber = GeneratePatientNumber(context);
            var username = GeneratePatientUsername(context, fullName);
            var emailAddress = string.IsNullOrWhiteSpace(email) ? GenerateEmailAddress(fullName) : email;
            var contactValue = ParseContactNumber(contactNumber);
            var nameParts = ParseName(fullName);

            var normalizedSex = NormalizeSexCode(sex);
            var trimmedAddress = string.IsNullOrWhiteSpace(address) ? "Not Provided" : address.Trim();
            var trimmedEmergencyContact = string.IsNullOrWhiteSpace(emergencyContact) ? "Not Provided" : emergencyContact.Trim();
            var trimmedRelationship = string.IsNullOrWhiteSpace(emergencyRelationship) ? "Unknown" : emergencyRelationship.Trim();
            var trimmedNationality = string.IsNullOrWhiteSpace(nationality) ? "Unknown" : nationality.Trim();

            var person = new EntityPerson
            {
                PersonId = personId,
                GivenName = nameParts.GivenName,
                LastName = nameParts.LastName,
                MiddleName = nameParts.MiddleName,
                Birthdate = DateOnly.FromDateTime(dateOfBirth.Date),
                Sex = normalizedSex,
                ContactNumber = contactValue,
                Address = trimmedAddress,
                EmergencyContact = trimmedEmergencyContact,
                RelationshipToEmergencyContact = trimmedRelationship,
                Email = emailAddress,
                Nationality = trimmedNationality
            };

            var patientEntity = new EntityPatient
            {
                PatientId = patientId,
                PersonId = personId,
                AssignedDoctorId = doctorId,
                AssignedNurseId = nurseId.Value,
                PatientIdNumber = patientNumber,
                Allergens = string.Empty,
                BloodType = DefaultBloodType,
                DateAdmitted = admitDateOverride ?? DateTime.Now,
                DateDischarged = currentlyAdmitted ? null : DateTime.Now,
                MedicalHistory = "Not Provided",
                CurrentMedications = null,
                RoomId = room.RoomId,
                MedicalRecords = null,
                BillId = billId,
                Status = approve ? (byte)1 : (byte)0
            };

            var billEntity = new EntityBill
            {
                BillId = billId,
                BillIdNumber = $"BILL-{billId:D5}",
                AssignedPatientId = patientId,
                Amount = 0m,
                Description = "Initial Admission",
                DateBilled = admitDateOverride ?? DateTime.Now,
                Status = approve ? (byte)1 : (byte)0,
                PaymentMethod = approve ? "Recorded" : null
            };

            var user = new EntityUser
            {
                UserId = userId,
                Username = username,
                Password = EncodePassword("patient123"),
                AssociatedPersonId = personId,
                UserRole = "patient"
            };

            context.People.Add(person);
            context.Patients.Add(patientEntity);
            context.Bills.Add(billEntity);
            context.Users.Add(user);
            context.SaveChanges();

            var createdUser = BuildUserQuery(context).Single(u => u.UserId == userId);
            var account = MapUserAccount(createdUser, CreateBillLookup(context));

            RaiseAdmissionsChanged();
            return account;
        }

        private IReadOnlyList<UserAccount> LoadAccounts()
        {
            using var context = CreateContext();

            var users = BuildUserQuery(context).ToList();
            var billsLookup = CreateBillLookup(context);

            return users.Select(user => MapUserAccount(user, billsLookup)).ToList();
        }

        private static IQueryable<EntityUser> BuildUserQuery(HospitalDbContext context)
        {
            return context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Doctors)
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                        .ThenInclude(pt => pt.AssignedDoctor)
                            .ThenInclude(d => d.Person)
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                        .ThenInclude(pt => pt.Room)
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                        .ThenInclude(pt => pt.Insurances)
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Staff)
                .AsSplitQuery()
                .AsNoTracking();
        }

        private static bool RequiresAdminProvisioning(HospitalDbContext context) =>
            !context.Users.AsNoTracking().Any(u => u.UserRole == "admin");

        private static ILookup<int, EntityBill> CreateBillLookup(HospitalDbContext context) =>
            context.Bills.AsNoTracking().ToLookup(b => b.AssignedPatientId);

        internal static UserAccount MapUserAccount(EntityUser user, ILookup<int, EntityBill> billsLookup)
        {
            if (user.AssociatedPerson is null)
            {
                throw new InvalidOperationException("User record is missing an associated person entry.");
            }

            var person = user.AssociatedPerson;
            var doctorEntity = person.Doctors.FirstOrDefault();
            var patientEntity = person.Patients.FirstOrDefault();
            var staffEntity = person.Staff.FirstOrDefault();

            var role = MapRole(user.UserRole);

            var account = new UserAccount
            {
                UserId = user.UserId,
                Username = user.Username,
                Password = user.Password,
                DisplayName = FormatFullName(person),
                Email = person.Email ?? string.Empty,
                Role = role,
                IsActive = true,
                IsSuperUser = role == UserRole.Admin
            };

            if (role == UserRole.Admin)
            {
                account.AdminProfile = new AdminProfile
                {
                    IsApproved = true,
                    ContactNumber = FormatContact(person.ContactNumber)
                };
            }

            if (role == UserRole.Staff)
            {
                account.StaffProfile = new StaffProfile
                {
                    IsApproved = staffEntity?.RegularStaff ?? false,
                    ContactNumber = FormatContact(person.ContactNumber)
                };
                account.IsActive = account.StaffProfile.IsApproved;
            }

            if (role == UserRole.Doctor && doctorEntity is not null)
            {
                var doctorStatus = MapDoctorStatus(doctorEntity.ApprovalStatus);
                account.DoctorProfile = new DoctorProfile
                {
                    Status = doctorStatus,
                    Department = doctorEntity.Department,
                    ContactNumber = FormatContact(person.ContactNumber),
                    LicenseNumber = doctorEntity.LicenseNumber,
                    Address = person.Address,
                    ApplicationDate = doctorEntity.EmploymentDate.ToDateTime(TimeOnly.MinValue)
                };
                account.IsActive = doctorStatus == DoctorStatus.Available;
            }

            if (role == UserRole.Patient && patientEntity is not null)
            {
                var patientBills = billsLookup[patientEntity.PatientId];
                var hasUnpaidBills = patientBills.Any(bill => !IsBillPaid(bill));

                account.PatientProfile = new PatientProfile
                {
                    IsApproved = patientEntity.Status != 0,
                    HasUnpaidBills = hasUnpaidBills,
                    PatientNumber = string.IsNullOrWhiteSpace(patientEntity.PatientIdNumber)
                        ? $"P-{patientEntity.PatientId:D5}"
                        : patientEntity.PatientIdNumber,
                    AdmitDate = patientEntity.DateAdmitted,
                    RoomAssignment = BuildRoomAssignment(patientEntity.Room),
                    ContactNumber = FormatContact(person.ContactNumber),
                    Address = person.Address,
                    EmergencyContact = BuildEmergencyContact(person),
                    InsuranceProvider = BuildInsuranceSummary(patientEntity.Insurances),
                    AssignedDoctorId = patientEntity.AssignedDoctorId,
                    AssignedDoctorName = patientEntity.AssignedDoctor?.Person is EntityPerson doctorPerson
                        ? FormatFullName(doctorPerson)
                        : "Unassigned",
                    DateOfBirth = person.Birthdate.ToDateTime(TimeOnly.MinValue),
                    IsCurrentlyAdmitted = patientEntity.DateDischarged is null
                };

                account.IsActive = patientEntity.Status != 0;
            }

            return account;
        }

        private static string NormalizeSexCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DefaultSexCode;
            }

            var trimmed = value.Trim();

            if (trimmed.Equals("male", StringComparison.OrdinalIgnoreCase))
            {
                return "M";
            }

            if (trimmed.Equals("female", StringComparison.OrdinalIgnoreCase))
            {
                return "F";
            }

            var first = char.ToUpperInvariant(trimmed[0]);
            return first is 'M' or 'F' ? first.ToString() : DefaultSexCode;
        }
    }
}
