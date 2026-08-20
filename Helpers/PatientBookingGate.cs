using IPTS.Models.Entites;

namespace IPTS.Helpers
{
    public static class PatientBookingGate
    {
        public const string BirthDateField = "birthDate";
        public const string PhoneNumberField = "phoneNumber";

        public static List<string> GetMissingFields(AppUser? user)
        {
            var missing = new List<string>();
            if (user?.Patient == null)
                return missing;

            if (!user.Patient.BirthDate.HasValue)
                missing.Add(BirthDateField);

            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                missing.Add(PhoneNumberField);

            return missing;
        }

        public static bool CanBook(AppUser? user)
        {
            return user?.EmailConfirmed == true
                && user.Patient != null
                && GetMissingFields(user).Count == 0;
        }
    }
}
