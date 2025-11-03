using System;
using System.Collections.Generic;
using System.Linq;
using Patient_Information_System_CS.Models;

namespace Patient_Information_System_CS.Services
{
    public sealed class AuthenticationService
    {
        private readonly HospitalDataService _dataService;

        public AuthenticationService(HospitalDataService dataService)
        {
            _dataService = dataService;
        }

        public AuthenticationResult Authenticate(string? username, string? password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return AuthenticationResult.Failed("Please enter your username and password.");
            }

            var normalizedUsername = username.Trim();
            var normalizedPassword = password.Trim();

            var account = _dataService.GetAccountByUsername(normalizedUsername);

            if (account is null || !account.PasswordMatches(normalizedPassword.AsSpan()))
            {
                return AuthenticationResult.Failed("Invalid username or password.");
            }

            if (!account.IsActive)
            {
                return AuthenticationResult.Failed("This account is currently deactivated. Please contact the administrator.");
            }

            switch (account.Role)
            {
                case UserRole.Admin:
                    if (account.AdminProfile is null || !account.AdminProfile.IsApproved)
                    {
                        return AuthenticationResult.Failed("Your admin account is awaiting approval.");
                    }
                    break;

                case UserRole.Staff:
                    if (account.StaffProfile is null || !account.StaffProfile.IsApproved)
                    {
                        return AuthenticationResult.Failed("Your staff account is awaiting approval.");
                    }
                    break;

                case UserRole.Doctor:
                    if (account.DoctorProfile is null)
                    {
                        return AuthenticationResult.Failed("Doctor profile is incomplete. Please contact the administrator.");
                    }
                    if (account.DoctorProfile.Status == DoctorStatus.OnHold)
                    {
                        return AuthenticationResult.Failed("Your doctor account is awaiting approval.");
                    }
                    break;

                case UserRole.Nurse:
                    if (account.NurseProfile is null)
                    {
                        return AuthenticationResult.Failed("Nurse profile is incomplete. Please contact the administrator.");
                    }
                    if (account.NurseProfile.Status == NurseStatus.OnHold)
                    {
                        return AuthenticationResult.Failed("Your nurse account is awaiting approval.");
                    }
                    break;

                case UserRole.Patient:
                    if (account.PatientProfile is null || !account.PatientProfile.IsApproved)
                    {
                        return AuthenticationResult.Failed("Your patient account is awaiting approval.");
                    }
                    break;

                default:
                    return AuthenticationResult.Failed("Unknown user role. Please contact support.");
            }

            return AuthenticationResult.Success(account);
        }

        public IReadOnlyCollection<UserAccount> GetAccounts() => _dataService.GetAllAccounts().ToList();
    }

    public sealed class AuthenticationResult
    {
        private AuthenticationResult(bool isAuthenticated, string message, UserAccount? account)
        {
            IsAuthenticated = isAuthenticated;
            Message = message;
            Account = account;
        }

        public bool IsAuthenticated { get; }
        public string Message { get; }
        public UserAccount? Account { get; }

        public static AuthenticationResult Success(UserAccount account) =>
            new(true, string.Empty, account);

        public static AuthenticationResult Failed(string message) =>
            new(false, message, null);
    }
}
