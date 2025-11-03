using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Patient_Information_System_CS.Data;
using Patient_Information_System_CS.Models;
using EntityBill = Patient_Information_System_CS.Models.Entities.Bill;
using EntityDoctor = Patient_Information_System_CS.Models.Entities.Doctor;
using EntityNurse = Patient_Information_System_CS.Models.Entities.Nurse;
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

        public UserAccount? GetAccountById(int userId)
        {
            using var context = CreateContext();

            var userEntity = BuildUserQuery(context)
                .SingleOrDefault(u => u.UserId == userId);

            if (userEntity is null)
            {
                return null;
            }

            return MapUserAccount(userEntity, CreateBillLookup(context));
        }

        public AccountProfileDetails GetAccountProfile(int userId)
        {
            using var context = CreateContext();

            var userEntity = context.Users
                .Include(u => u.AssociatedPerson)
                .AsNoTracking()
                .SingleOrDefault(u => u.UserId == userId);

            if (userEntity?.AssociatedPerson is null)
            {
                throw new InvalidOperationException("Unable to locate the requested user profile.");
            }

            var person = userEntity.AssociatedPerson;

            return new AccountProfileDetails
            {
                UserId = userEntity.UserId,
                Username = userEntity.Username,
                GivenName = person.GivenName,
                MiddleName = person.MiddleName,
                LastName = person.LastName,
                Suffix = person.Suffix
            };
        }

        public UserAccount UpdateAccountProfile(AccountProfileUpdate update)
        {
            if (update is null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            var desiredUsername = update.Username?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(update.GivenName) || string.IsNullOrWhiteSpace(update.LastName))
            {
                throw new ArgumentException("Given name and last name are required to update the account profile.");
            }

            if (string.IsNullOrWhiteSpace(desiredUsername))
            {
                throw new ArgumentException("Username is required to update the account profile.");
            }

            using var context = CreateContext(tracking: true);

            var userEntity = context.Users
                .Include(u => u.AssociatedPerson)
                .SingleOrDefault(u => u.UserId == update.UserId);

            if (userEntity?.AssociatedPerson is null)
            {
                throw new InvalidOperationException("Unable to locate the requested user profile.");
            }

            var person = userEntity.AssociatedPerson;

            if (!string.Equals(userEntity.Username, desiredUsername, StringComparison.OrdinalIgnoreCase))
            {
                var normalizedDesiredUsername = desiredUsername.ToUpperInvariant();
                var isTaken = context.Users
                    .AsNoTracking()
                    .Any(u => u.UserId != update.UserId && (u.Username ?? string.Empty).ToUpper() == normalizedDesiredUsername);

                if (isTaken)
                {
                    throw new InvalidOperationException("The chosen username is already in use.");
                }

                userEntity.Username = desiredUsername;
            }

            person.GivenName = update.GivenName.Trim();
            person.MiddleName = string.IsNullOrWhiteSpace(update.MiddleName) ? null : update.MiddleName.Trim();
            person.LastName = update.LastName.Trim();
            person.Suffix = string.IsNullOrWhiteSpace(update.Suffix) ? null : update.Suffix.Trim();

            if (!string.IsNullOrWhiteSpace(update.NewPassword))
            {
                userEntity.Password = EncodePassword(update.NewPassword.Trim());
            }

            context.SaveChanges();

            var refreshedUser = BuildUserQuery(context).Single(u => u.UserId == update.UserId);
            var account = MapUserAccount(refreshedUser, CreateBillLookup(context));

            RaiseAdmissionsChanged();
            return account;
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

        public IEnumerable<UserAccount> GetAllNurses() =>
            LoadAccounts().Where(account => account.Role == UserRole.Nurse);

        public IEnumerable<UserAccount> GetActiveNurses() =>
            GetAllNurses().Where(account => account.NurseProfile is { Status: NurseStatus.Available });

        public IEnumerable<UserAccount> GetUnavailableNurses() =>
            GetAllNurses().Where(account => account.NurseProfile is { Status: NurseStatus.NotAvailable });

        public IEnumerable<UserAccount> GetPendingNurses() =>
            GetAllNurses().Where(account => account.NurseProfile is { Status: NurseStatus.OnHold });

        public IEnumerable<UserAccount> GetAllPatients() =>
            LoadAccounts().Where(account => account.Role == UserRole.Patient);

        public IEnumerable<UserAccount> GetPatientsForDoctor(int doctorId)
        {
            using var context = CreateContext();
            var normalizedDoctorId = NormalizeDoctorId(context, doctorId);

            if (!normalizedDoctorId.HasValue)
            {
                return Array.Empty<UserAccount>();
            }

            return LoadAccounts().Where(account => account.Role == UserRole.Patient &&
                                                   account.PatientProfile is not null &&
                                                   account.PatientProfile.AssignedDoctorId == normalizedDoctorId.Value);
        }

        public IEnumerable<UserAccount> GetApprovedStaff() =>
            LoadAccounts().Where(account => (account.Role == UserRole.Staff || account.Role == UserRole.Admin) &&
                                            ((account.AdminProfile?.IsApproved ?? false) ||
                                             (account.StaffProfile?.IsApproved ?? false)));

        public IEnumerable<UserAccount> GetPendingStaff() =>
            LoadAccounts().Where(account => account.Role == UserRole.Staff &&
                                            !(account.StaffProfile?.IsApproved ?? false) &&
                                            !(account.StaffProfile?.HasCompletedOnboarding ?? false));

        public IEnumerable<UserAccount> GetInactiveStaff() =>
            LoadAccounts().Where(account => account.Role == UserRole.Staff &&
                                            !(account.StaffProfile?.IsApproved ?? false) &&
                                            (account.StaffProfile?.HasCompletedOnboarding ?? false));

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
            staffEntity.SupervisorId ??= -1;
            context.SaveChanges();
            RaiseAdmissionsChanged();
        }

        public void ActivateStaffAccount(UserAccount account)
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
            if (!staffEntity.SupervisorId.HasValue || staffEntity.SupervisorId <= 0)
            {
                staffEntity.SupervisorId = -1;
            }
            context.SaveChanges();
            RaiseAdmissionsChanged();
        }

        public void DeactivateStaffAccount(UserAccount account)
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

            staffEntity.RegularStaff = false;
            staffEntity.SupervisorId = -1;
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

        public void ApproveNurse(UserAccount nurse)
        {
            using var context = CreateContext(tracking: true);

            var userEntity = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Nurses)
                        .ThenInclude(n => n.Patients)
                .SingleOrDefault(u => u.UserId == nurse.UserId);

            var nurseEntity = userEntity?.AssociatedPerson?.Nurses.FirstOrDefault();
            if (nurseEntity is null)
            {
                return;
            }

            ApplyNurseStatus(nurseEntity, NurseStatus.Available);
            if (string.IsNullOrWhiteSpace(nurseEntity.SupervisorId))
            {
                nurseEntity.SupervisorId = "-1";
            }
            context.SaveChanges();
            RaiseAdmissionsChanged();
        }

        public void RejectNurse(UserAccount nurse)
        {
            using var context = CreateContext(tracking: true);

            var userEntity = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Nurses)
                        .ThenInclude(n => n.Patients)
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Users)
                .SingleOrDefault(u => u.UserId == nurse.UserId);

            if (userEntity is null)
            {
                return;
            }

            var person = userEntity.AssociatedPerson;
            foreach (var nurseEntry in person.Nurses.ToList())
            {
                context.Nurses.Remove(nurseEntry);
            }

            context.Users.Remove(userEntity);

            if (person.Users.Count <= 1 &&
                !person.Staff.Any() &&
                !person.Doctors.Any() &&
                !person.Nurses.Any() &&
                !person.Patients.Any())
            {
                context.People.Remove(person);
            }

            context.SaveChanges();
            RaiseAdmissionsChanged();
        }

        public void ToggleNurseAvailability(UserAccount nurse)
        {
            using var context = CreateContext(tracking: true);

            var userEntity = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Nurses)
                        .ThenInclude(n => n.Patients)
                .SingleOrDefault(u => u.UserId == nurse.UserId);

            var nurseEntity = userEntity?.AssociatedPerson?.Nurses.FirstOrDefault();
            if (nurseEntity is null)
            {
                return;
            }

            var current = MapNurseStatus(nurseEntity);
            var next = current == NurseStatus.Available ? NurseStatus.NotAvailable : NurseStatus.Available;
            ApplyNurseStatus(nurseEntity, next);

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

        public IReadOnlyList<ExistingPatientOption> GetExistingPatientOptions()
        {
            return GetAllPatients()
                .Where(account => account.PatientProfile is not null)
                .Select(account => new ExistingPatientOption
                {
                    UserId = account.UserId,
                    DisplayName = account.DisplayName,
                    ContactNumber = account.PatientProfile?.ContactNumber ?? string.Empty,
                    PatientNumber = account.PatientProfile?.PatientNumber ?? string.Empty,
                    IsCurrentlyAdmitted = account.PatientProfile?.IsCurrentlyAdmitted ?? false
                })
                .OrderBy(option => option.IsCurrentlyAdmitted)
                .ThenBy(option => option.DisplayName)
                .ToList();
        }

        public IEnumerable<UserAccount> GetDeactivatedPatients() =>
            LoadAccounts().Where(account => account.Role == UserRole.Patient &&
                                            (!account.IsActive || (account.PatientProfile?.HasUnpaidBills ?? false)));

        public BillingRecord DischargePatient(UserAccount patient, DischargeBillingRequest request)
        {
            if (patient is null)
            {
                throw new ArgumentNullException(nameof(patient));
            }

            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using var context = CreateContext(tracking: true);

            var userEntity = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                        .ThenInclude(pt => pt.AssignedDoctor)
                            .ThenInclude(d => d.Person)
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                        .ThenInclude(pt => pt.AssignedNurse)
                            .ThenInclude(n => n.Person)
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                        .ThenInclude(pt => pt.Room)
                .SingleOrDefault(u => u.UserId == patient.UserId);

            var patientEntity = userEntity?.AssociatedPerson?.Patients.FirstOrDefault();
            if (patientEntity is null)
            {
                throw new InvalidOperationException("Unable to locate the patient record for discharge.");
            }

            var dischargeDate = request.DischargeDate ?? DateTime.Now;
            patientEntity.DateDischarged = dischargeDate;
            patientEntity.Status = 1;

            var total = request.TotalAmount;
            var billId = NextBillId(context);

            var bill = new EntityBill
            {
                BillId = billId,
                BillIdNumber = $"BILL-{billId:D5}",
                AssignedPatientId = patientEntity.PatientId,
                Amount = total,
                Description = BuildDischargeDescription(request),
                DateBilled = dischargeDate,
                Status = request.MarkAsPaid ? (byte)1 : (byte)0,
                PaymentMethod = request.MarkAsPaid ? "Discharge Desk" : null
            };

            patientEntity.BillId = billId;

            context.Bills.Add(bill);
            context.SaveChanges();

            var savedBill = context.Bills
                .Include(b => b.AssignedPatient)
                    .ThenInclude(p => p.Person)
                .Include(b => b.AssignedPatient)
                    .ThenInclude(p => p.AssignedDoctor)
                        .ThenInclude(d => d.Person)
                .Include(b => b.AssignedPatient)
                    .ThenInclude(p => p.AssignedNurse)
                        .ThenInclude(n => n.Person)
                .AsNoTracking()
                .Single(b => b.BillId == billId);

            RaiseAdmissionsChanged();
            return MapBill(savedBill);
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
                           UserAccount? nurse,
                           string roomAssignment,
                           string? emailAddress = null,
                           DateTime? admitDateOverride = null,
                           string sex = DefaultSexCode,
                           string emergencyRelationship = "Unknown",
                           string nationality = "Unknown")
        {
            var email = string.IsNullOrWhiteSpace(emailAddress)
                ? GenerateEmailAddress(fullName)
                : emailAddress.Trim();
            var doctorId = doctor?.UserId;

            return CreatePatientAccount(fullName,
                                        email,
                                        contactNumber,
                                        address,
                                        dateOfBirth,
                                        approve: true,
                                        currentlyAdmitted: true,
                                        doctorId,
                                        nurse?.UserId,
                                        insuranceProvider,
                                        emergencyContact,
                                        roomAssignment,
                                        sex,
                                        emergencyRelationship,
                                        nationality,
                                        admitDateOverride: admitDateOverride);
        }

        public UserAccount ReadmitExistingPatient(ExistingPatientAdmissionRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using var context = CreateContext(tracking: true);

            var userEntity = context.Users
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                        .ThenInclude(pt => pt.AssignedDoctor)
                            .ThenInclude(d => d.Person)
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                        .ThenInclude(pt => pt.AssignedNurse)
                            .ThenInclude(n => n.Person)
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                        .ThenInclude(pt => pt.Room)
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                        .ThenInclude(pt => pt.Insurances)
                .SingleOrDefault(u => u.UserId == request.UserId);

            var person = userEntity?.AssociatedPerson;
            var patientEntity = person?.Patients.FirstOrDefault();
            if (userEntity is null || person is null || patientEntity is null)
            {
                throw new InvalidOperationException("Unable to locate the existing patient record for admission.");
            }

            var doctorId = ResolveDoctorId(context, request.AssignedDoctorUserId) ?? patientEntity.AssignedDoctorId;
            var nurseId = ResolveNurseId(context, request.AssignedNurseUserId) ?? (int?)patientEntity.AssignedNurseId ?? ResolveFallbackNurse(context);
            if (nurseId is null)
            {
                throw new InvalidOperationException("No nurse records are available. Please seed the Nurse table before admitting patients.");
            }
            var room = ResolveRoom(context, request.RoomAssignment);

            person.ContactNumber = ParseContactNumber(request.ContactNumber);
            if (!string.IsNullOrWhiteSpace(request.Address))
            {
                person.Address = request.Address.Trim();
            }

            person.EmergencyContact = string.IsNullOrWhiteSpace(request.EmergencyContact)
                ? person.EmergencyContact
                : request.EmergencyContact.Trim();
            person.RelationshipToEmergencyContact = string.IsNullOrWhiteSpace(request.EmergencyRelationship)
                ? person.RelationshipToEmergencyContact
                : request.EmergencyRelationship.Trim();

            patientEntity.AssignedDoctorId = doctorId;
            patientEntity.AssignedNurseId = nurseId.Value;
            patientEntity.RoomId = room.RoomId;
            patientEntity.DateAdmitted = request.AdmitDateOverride ?? DateTime.Now;
            patientEntity.DateDischarged = null;
            patientEntity.Status = 1;

            var insuranceProvider = string.IsNullOrWhiteSpace(request.InsuranceProvider)
                ? "No Insurance"
                : request.InsuranceProvider.Trim();
            var insurance = patientEntity.Insurances.FirstOrDefault();
            if (insurance is not null)
            {
                insurance.ProviderName = insuranceProvider;
            }

            context.SaveChanges();

            var refreshedUser = BuildUserQuery(context).Single(u => u.UserId == request.UserId);
            var account = MapUserAccount(refreshedUser, CreateBillLookup(context));

            RaiseAdmissionsChanged();
            return account;
        }

        public UserAccount? GetDoctorById(int? doctorIdentifier)
        {
            if (doctorIdentifier is null)
            {
                return null;
            }

            var doctors = GetAllDoctors().ToList();

            var matchByUser = doctors.FirstOrDefault(account => account.UserId == doctorIdentifier.Value);
            if (matchByUser is not null)
            {
                return matchByUser;
            }

            return doctors.FirstOrDefault(account => account.DoctorProfile?.DoctorId == doctorIdentifier.Value);
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
                              string nationality,
                              UserRole targetRole = UserRole.Staff)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException("Full name is required", nameof(fullName));
            }

            using var context = CreateContext(tracking: true);

            var personId = NextPersonId(context);
            var staffId = NextStaffId(context);
            var userId = NextUserId(context);

            var roleKey = targetRole == UserRole.Admin ? "admin" : "staff";
            var username = GenerateUniqueUsername(context, fullName, roleKey);
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
                SupervisorId = -1,
                Salary = 0m,
                PersonId = personId
            };

            var user = new EntityUser
            {
                UserId = userId,
                Username = username,
                Password = EncodePassword("changeme"),
                AssociatedPersonId = personId,
                UserRole = roleKey
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

        public UserAccount CreateNurseAccount(string fullName,
                                               string email,
                                               string contactNumber,
                                               string department,
                                               string specialization,
                                               string licenseNumber,
                                               string address,
                                               NurseStatus status,
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
            var nurseId = NextNurseId(context);
            var userId = NextUserId(context);

            var username = GenerateUniqueUsername(context, fullName, "nurse");
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

            var nurseEntity = new EntityNurse
            {
                NurseId = nurseId,
                NurseIdNumber = $"NUR-{nurseId:D5}",
                LicenseNumber = string.IsNullOrWhiteSpace(licenseNumber) ? $"NUR-LIC-{nurseId:D5}" : licenseNumber.Trim(),
                Department = string.IsNullOrWhiteSpace(department) ? "General Medicine" : department.Trim(),
                Specialization = string.IsNullOrWhiteSpace(specialization) ? "General" : specialization.Trim(),
                EmploymentDate = DateOnly.FromDateTime(DateTime.Today),
                RegularStaff = false,
                ResidencyDate = null,
                SupervisorId = null,
                Salary = 0m,
                PersonId = personId
            };

            ApplyNurseStatus(nurseEntity, status);
            if (nurseEntity.RegularStaff && string.IsNullOrWhiteSpace(nurseEntity.SupervisorId))
            {
                nurseEntity.SupervisorId = "-1";
            }

            var user = new EntityUser
            {
                UserId = userId,
                Username = username,
                Password = EncodePassword("nurse123"),
                AssociatedPersonId = personId,
                UserRole = "nurse"
            };

            context.People.Add(person);
            context.Nurses.Add(nurseEntity);
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
                            int? assignedNurseUserId,
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
            var nurseId = ResolveNurseId(context, assignedNurseUserId) ?? ResolveFallbackNurse(context);
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
                    .ThenInclude(p => p.Nurses)
                        .ThenInclude(n => n.Patients)
                            .ThenInclude(p => p.Person)
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Doctors)
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                        .ThenInclude(pt => pt.AssignedDoctor)
                            .ThenInclude(d => d.Person)
                .Include(u => u.AssociatedPerson)
                    .ThenInclude(p => p.Patients)
                        .ThenInclude(pt => pt.AssignedNurse)
                            .ThenInclude(n => n.Person)
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
            var nurseEntity = person.Nurses.FirstOrDefault();
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

            if ((role == UserRole.Staff || role == UserRole.Admin) && staffEntity is not null)
            {
                var isActiveStaff = staffEntity.RegularStaff;
                var hasCompletedOnboarding = staffEntity.SupervisorId.HasValue && staffEntity.SupervisorId > 0;
                account.StaffProfile = new StaffProfile
                {
                    IsApproved = isActiveStaff,
                    ContactNumber = FormatContact(person.ContactNumber),
                    HasCompletedOnboarding = hasCompletedOnboarding
                };
                if (role == UserRole.Staff)
                {
                    account.IsActive = isActiveStaff;
                }
            }

            if (role == UserRole.Doctor && doctorEntity is not null)
            {
                var doctorStatus = MapDoctorStatus(doctorEntity.ApprovalStatus);
                account.DoctorProfile = new DoctorProfile
                {
                    DoctorId = doctorEntity.DoctorId,
                    Status = doctorStatus,
                    Department = doctorEntity.Department,
                    ContactNumber = FormatContact(person.ContactNumber),
                    LicenseNumber = doctorEntity.LicenseNumber,
                    DoctorNumber = string.IsNullOrWhiteSpace(doctorEntity.DoctorIdNumber)
                        ? $"DOC-{doctorEntity.DoctorId:D5}"
                        : doctorEntity.DoctorIdNumber,
                    Address = person.Address,
                    ApplicationDate = doctorEntity.EmploymentDate.ToDateTime(TimeOnly.MinValue)
                };
                account.IsActive = doctorStatus == DoctorStatus.Available;
            }

            if (role == UserRole.Nurse && nurseEntity is not null)
            {
                var nurseStatus = MapNurseStatus(nurseEntity);
                var assignedPatientNames = nurseEntity.Patients
                    .Select(p => p.Person is EntityPerson patientPerson ? FormatFullName(patientPerson) : $"Patient #{p.PatientId:D4}")
                    .OrderBy(name => name)
                    .ToList();

                var assignmentsSummary = assignedPatientNames.Count == 0
                    ? "No assignments"
                    : string.Join(", ", assignedPatientNames.Take(5));

                if (assignedPatientNames.Count > 5)
                {
                    assignmentsSummary += $" (+{assignedPatientNames.Count - 5} more)";
                }

                account.NurseProfile = new NurseProfile
                {
                    NurseId = nurseEntity.NurseId,
                    Status = nurseStatus,
                    Department = nurseEntity.Department ?? string.Empty,
                    Specialization = nurseEntity.Specialization ?? string.Empty,
                    ContactNumber = FormatContact(person.ContactNumber),
                    LicenseNumber = nurseEntity.LicenseNumber ?? string.Empty,
                    NurseNumber = string.IsNullOrWhiteSpace(nurseEntity.NurseIdNumber)
                        ? $"NUR-{nurseEntity.NurseId:D5}"
                        : nurseEntity.NurseIdNumber,
                    EmploymentDate = nurseEntity.EmploymentDate.ToDateTime(TimeOnly.MinValue),
                    AssignedPatientsCount = assignedPatientNames.Count,
                    AssignedPatientsSummary = assignmentsSummary
                };

                account.IsActive = nurseStatus == NurseStatus.Available;
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
                    EmergencyContact = person.EmergencyContact ?? string.Empty,
                    EmergencyRelationship = string.IsNullOrWhiteSpace(person.RelationshipToEmergencyContact)
                        ? "Unknown"
                        : person.RelationshipToEmergencyContact,
                    InsuranceProvider = BuildInsuranceSummary(patientEntity.Insurances),
                    AssignedDoctorId = patientEntity.AssignedDoctorId,
                    AssignedDoctorName = patientEntity.AssignedDoctor?.Person is EntityPerson doctorPerson
                        ? FormatFullName(doctorPerson)
                        : "Unassigned",
                    AssignedNurseId = patientEntity.AssignedNurseId,
                    AssignedNurseName = patientEntity.AssignedNurse?.Person is EntityPerson nursePerson
                        ? FormatFullName(nursePerson)
                        : "Unassigned",
                    DateOfBirth = person.Birthdate.ToDateTime(TimeOnly.MinValue),
                    IsCurrentlyAdmitted = patientEntity.DateDischarged is null,
                    Nationality = string.IsNullOrWhiteSpace(person.Nationality) ? "Unknown" : person.Nationality,
                    Sex = NormalizeSexCode(person.Sex)
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

        private const int MaxBillDescriptionLength = 100;

        private static string BuildDischargeDescription(DischargeBillingRequest request)
        {
            var components = new List<string>();

            if (request.RoomCharge > 0)
            {
                components.Add($"Room: {request.RoomCharge:C}");
            }

            if (request.DoctorFee > 0)
            {
                components.Add($"Doctor: {request.DoctorFee:C}");
            }

            if (request.MedicineCost > 0)
            {
                components.Add($"Medicine: {request.MedicineCost:C}");
            }

            if (request.OtherCharges > 0)
            {
                components.Add($"Other: {request.OtherCharges:C}");
            }

            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                components.Add(request.Notes.Trim());
            }

            var description = components.Count == 0 ? "Discharge Billing" : string.Join("; ", components);
            return NormalizeBillDescription(description);
        }

        private static string NormalizeBillDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return "Discharge Billing";
            }

            var normalized = description
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Trim();

            if (normalized.Length <= MaxBillDescriptionLength)
            {
                return normalized;
            }

            const string ellipsis = "...";
            var limit = MaxBillDescriptionLength - ellipsis.Length;
            if (limit <= 0)
            {
                return normalized[..Math.Min(MaxBillDescriptionLength, normalized.Length)];
            }

            return normalized[..limit].TrimEnd() + ellipsis;
        }
    }
}
