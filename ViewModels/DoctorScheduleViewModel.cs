namespace IPTS.ViewModels
{
    public class DoctorScheduleViewModel
    {
        public DoctorViewModel Doctor { get; set; }
        public List<AppointmentTimeSlotViewModel> TimeSlots { get; set; }
        public DateTime SelectedDate { get; set; }
    }

}
