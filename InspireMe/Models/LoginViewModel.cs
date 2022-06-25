using System.ComponentModel.DataAnnotations;

namespace InspireMe.Models
{
    public class LoginViewModel
    {
        
        [Required(ErrorMessage = "Bu alan gereklidir")]
        [Display(Name = "Parola")]
        [DataType(DataType.Password,ErrorMessage = "Lütfen geçerli bir parola seçiniz")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Bu alan gereklidir")]
        [Display(Name = "E-Posta")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        
        [Display(Name = "Beni Hatırla")]
        public bool RememberMe { get; set; }
    }
}
