using System.ComponentModel.DataAnnotations;

namespace InspireMe.Models
{
    public class ChangePasswordViewModel
    {

        [Required(ErrorMessage = "Bu alan gereklidir")]
        [Display(Name = "Eski Parola")]
        [DataType(DataType.Password, ErrorMessage = "Lütfen geçerli bir parola seçiniz")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Bu alan gereklidir")]
        [Display(Name = "Yeni Parola")]
        [DataType(DataType.Password,ErrorMessage = "Lütfen geçerli bir parola seçiniz")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Bu alan gereklidir")]
        [Display(Name = "Parolayı Onayla")]
        [Compare("Password",ErrorMessage ="Parolalar uyuşmuyor")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
       
    }
}
