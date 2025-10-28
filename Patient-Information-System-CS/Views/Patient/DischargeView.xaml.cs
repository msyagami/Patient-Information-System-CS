using System.Windows.Controls;

namespace Patient_Information_System_CS.Views.Patient
{
    public partial class DischargeView : UserControl
    {
        public DischargeView()
        {
            InitializeComponent();
        }
    }

    // Placeholder class for invoice items
    public class InvoiceItem
    {
        public string Item { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
    }
}
