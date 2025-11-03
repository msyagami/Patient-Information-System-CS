using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Patient_Information_System_CS.Models;

namespace Patient_Information_System_CS.Services
{
    public sealed partial class HospitalDataService
    {
        public IReadOnlyList<RoomOption> GetRoomOptions()
        {
            using var context = CreateContext();

            return context.Rooms
                .AsNoTracking()
                .OrderBy(r => r.RoomNumber)
                .ThenBy(r => r.RoomType)
                .Select(r => new RoomOption
                {
                    RoomId = r.RoomId,
                    RoomNumber = r.RoomNumber,
                    RoomType = r.RoomType,
                    Capacity = r.Capacity
                })
                .ToList();
        }
    }
}
