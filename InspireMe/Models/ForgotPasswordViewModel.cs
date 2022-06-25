using System.ComponentModel.DataAnnotations;

namespace InspireMe.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Bu alan gereklidir")]
        [Display(Name = "E-Posta")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
    }
}
