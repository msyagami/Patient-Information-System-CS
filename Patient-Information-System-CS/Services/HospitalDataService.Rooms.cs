using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Patient_Information_System_CS.Models;
using Patient_Information_System_CS.Models.Entities;

namespace Patient_Information_System_CS.Services
{
    public sealed partial class HospitalDataService
    {
        public IReadOnlyList<RoomStatus> GetRoomStatuses()
        {
            using var context = CreateContext();

            var rooms = context.Rooms
                .AsNoTracking()
                .Include(r => r.Patients)
                    .ThenInclude(p => p.Person)
                .Include(r => r.Patients)
                    .ThenInclude(p => p.AssignedDoctor)
                        .ThenInclude(d => d.Person)
                .Include(r => r.Patients)
                    .ThenInclude(p => p.AssignedNurse)
                        .ThenInclude(n => n.Person)
                .OrderBy(r => r.RoomNumber)
                .ThenBy(r => r.RoomType)
                .ToList();

            return rooms.Select(r => new RoomStatus
            {
                RoomId = r.RoomId,
                RoomNumber = r.RoomNumber,
                RoomType = r.RoomType,
                Capacity = r.Capacity,
                Occupants = r.Patients
                    .Where(p => p.DateDischarged is null)
                    .OrderBy(p => p.DateAdmitted)
                    .Select(p => new RoomOccupantInfo
                    {
                        PatientId = p.PatientId,
                        PatientName = FormatPersonName(p.Person),
                        DoctorName = FormatDoctorName(p.AssignedDoctor),
                        NurseName = FormatNurseName(p.AssignedNurse)
                    })
                    .ToList()
            }).ToList();
        }

        public RoomStatus AddRoom(int roomNumber, string roomType, int capacity)
        {
            if (roomNumber <= 0)
            {
                throw new ArgumentException("Room number must be a positive integer.", nameof(roomNumber));
            }

            if (string.IsNullOrWhiteSpace(roomType))
            {
                throw new ArgumentException("Room type is required.", nameof(roomType));
            }

            if (capacity <= 0 || capacity > byte.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Room capacity must be between 1 and 255.");
            }

            using var context = CreateContext(tracking: true);

            if (context.Rooms.Any(r => r.RoomNumber == roomNumber))
            {
                throw new InvalidOperationException($"Room number {roomNumber} already exists.");
            }

            var roomId = NextRoomId(context);

            var room = new Room
            {
                RoomId = roomId,
                RoomNumber = (short)roomNumber,
                RoomType = roomType.Trim(),
                Capacity = (byte)capacity
            };

            context.Rooms.Add(room);
            context.SaveChanges();

            RaiseAdmissionsChanged();

            return new RoomStatus
            {
                RoomId = room.RoomId,
                RoomNumber = room.RoomNumber,
                RoomType = room.RoomType,
                Capacity = room.Capacity,
                Occupants = Array.Empty<RoomOccupantInfo>()
            };
        }

        private static string FormatPersonName(Person? person)
        {
            if (person is null)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(person.GivenName))
            {
                parts.Add(person.GivenName.Trim());
            }

            if (!string.IsNullOrWhiteSpace(person.LastName))
            {
                parts.Add(person.LastName.Trim());
            }

            return parts.Count == 0 ? "(Unknown)" : string.Join(" ", parts);
        }

        private static string FormatDoctorName(Doctor? doctor)
        {
            if (doctor?.Person is null)
            {
                return string.Empty;
            }

            return FormatPersonName(doctor.Person);
        }

        private static string FormatNurseName(Nurse? nurse)
        {
            if (nurse?.Person is null)
            {
                return string.Empty;
            }

            return FormatPersonName(nurse.Person);
        }
    }
}
