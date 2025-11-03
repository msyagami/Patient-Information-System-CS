using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Patient_Information_System_CS.Services;
using Patient_Information_System_CS.Views.Admin.Dialogs;

namespace Patient_Information_System_CS.Views.Admin
{
    public partial class RoomsView : UserControl
    {
        private readonly HospitalDataService _dataService = HospitalDataService.Instance;

        public RoomsView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _dataService.AdmissionsChanged += OnAdmissionsChanged;
            RefreshRoomData();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _dataService.AdmissionsChanged -= OnAdmissionsChanged;
        }

        private void OnAdmissionsChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(RefreshRoomData);
        }

        private void RefreshRoomData()
        {
            var rooms = _dataService.GetRoomStatuses().ToList();

            var occupied = rooms.Where(r => r.OccupiedCount > 0).ToList();
            var available = rooms.Where(r => r.OccupiedCount == 0).ToList();

            RoomsInUseGrid.ItemsSource = occupied;
            AvailableRoomsGrid.ItemsSource = available;

            var totalAvailableSlots = rooms.Sum(r => r.AvailableSlots);
            var totalOccupiedSlots = rooms.Sum(r => r.OccupiedCount);

            AvailableRoomsCountTextBlock.Text = available.Count.ToString();
            AvailableRoomsDetailTextBlock.Text = totalAvailableSlots == 1
                ? "1 available bed"
                : $"{totalAvailableSlots} available beds";

            OccupiedRoomsCountTextBlock.Text = occupied.Count.ToString();
            OccupiedRoomsDetailTextBlock.Text = totalOccupiedSlots == 1
                ? "1 patient admitted"
                : $"{totalOccupiedSlots} patients admitted";
        }

        private void AddRoomButton_Click(object sender, RoutedEventArgs e)
        {
            var owner = Window.GetWindow(this);
            var dialog = new AddRoomWindow
            {
                Owner = owner
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _dataService.AddRoom(dialog.RoomNumber, dialog.RoomType, dialog.Capacity);
                RefreshRoomData();
                var message = $"Room {dialog.RoomNumber} added successfully.";
                var ownerWindow = owner ?? Application.Current.MainWindow;
                if (ownerWindow is not null)
                {
                    MessageBox.Show(ownerWindow, message, "Room Added", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(message, "Room Added", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                var ownerWindow = owner ?? Application.Current.MainWindow;
                if (ownerWindow is not null)
                {
                    MessageBox.Show(ownerWindow,
                                    ex.Message,
                                    "Unable to Add Room",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show(ex.Message,
                                    "Unable to Add Room",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
            }
        }
    }
}
