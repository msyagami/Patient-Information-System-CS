using System.Windows.Controls;

namespace Patient_Information_System_CS.Views.Patient
{
    public partial class PatientAppointmentsView : UserControl
    {
        public PatientAppointmentsView()
        {
            InitializeComponent();
        }
    }

    // Placeholder class for appointment data
    public class AppointmentItem
    {
        public string DateTime { get; set; } = string.Empty;
        public string Doctor { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
