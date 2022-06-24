using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace InspireMe.Data
{
    public class DateTimeExistsAttribute : ValidationAttribute
    {
        public DateTimeExistsAttribute() { }
           

        

        public string GetErrorMessage() =>
            $"Bu kayıt zaten mevcut!!";

        protected override ValidationResult? IsValid(
            object? value, ValidationContext validationContext)
        {
            var availableDates = (AvailableDate)validationContext.ObjectInstance;
            var releaseYear = (int)value;
            
           

            return ValidationResult.Success;
        }
    }
}
