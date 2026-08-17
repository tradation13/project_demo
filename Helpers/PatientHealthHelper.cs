using IPTS.Models.Entites;
using IPTS.Models.Enums;
using IPTS.ViewModels;

namespace IPTS.Helpers
{
    public static class PatientHealthHelper
    {
        public static void CopyTo(this IPatientHealthFields source, Patient patient)
        {
            patient.Weight = source.Weight;
            patient.Height = source.Height;
            patient.BloodGroup = source.BloodGroup;
            patient.IsSmoker = source.IsSmoker;
            patient.HasChronicDisease = source.HasChronicDisease;
        }

        public static string GetBloodGroupResourceKey(EnBloodGroup? bloodGroup)
        {
            return bloodGroup switch
            {
                EnBloodGroup.APositive => "BloodGroup_APositive",
                EnBloodGroup.ANegative => "BloodGroup_ANegative",
                EnBloodGroup.BPositive => "BloodGroup_BPositive",
                EnBloodGroup.BNegative => "BloodGroup_BNegative",
                EnBloodGroup.OPositive => "BloodGroup_OPositive",
                EnBloodGroup.ONegative => "BloodGroup_ONegative",
                EnBloodGroup.ABPositive => "BloodGroup_ABPositive",
                EnBloodGroup.ABNegative => "BloodGroup_ABNegative",
                _ => string.Empty
            };
        }
    }
}
