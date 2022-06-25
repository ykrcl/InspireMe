using System.ComponentModel.DataAnnotations;

namespace InspireMe.Models
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Bu alan gereklidir")]
        [Display(Name = "Parola")]
        [DataType(DataType.Password, ErrorMessage = "Lütfen geçerli bir parola seçiniz")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Bu alan gereklidir")]
        [Display(Name = "Parolayı Onayla")]
        [Compare("Password", ErrorMessage = "Parolalar uyuşmuyor")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string ResetToken { get; set; }
    }
}
