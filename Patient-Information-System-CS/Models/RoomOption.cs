namespace Patient_Information_System_CS.Models
{
    public sealed class RoomOption
    {
        public int RoomId { get; set; }
        public int RoomNumber { get; set; }
        public string RoomType { get; set; } = string.Empty;
        public int Capacity { get; set; }

        public string DisplayLabel => RoomNumber == 0
            ? RoomType
            : $"Room {RoomNumber} - {RoomType}";
    }
}
