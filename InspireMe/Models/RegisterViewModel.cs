using System.ComponentModel.DataAnnotations;

namespace InspireMe.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Bu alan gereklidir")]
        [Display(Name = "Kullanıcı Adı")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Bu alan gereklidir")]
        [Display(Name = "Parola")]
        [DataType(DataType.Password,ErrorMessage = "Lütfen geçerli bir parola seçiniz")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Bu alan gereklidir")]
        [Display(Name = "Parolayı Onayla")]
        [Compare("Password",ErrorMessage ="Parolalar uyuşmuyor")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
        [Required(ErrorMessage = "Bu alan gereklidir")]
        [Display(Name = "E-Posta")]
        [DataType(DataType.EmailAddress, ErrorMessage = "Lütfen geçerli bir e-posta giriniz")]
        public string Email { get; set; }

        [Display(Name = "Telefon")]
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(@"^[0-9]{3}\-?\s?[0-9]{3}\-?\s?[0-9]{2}\-?\s?[0-9]{2}$",ErrorMessage = "Lütfen geçerli bir telefon numarası girniz")]
        public string? PhoneNumber { get; set; }


        [Display(Name = "Danışman Üyeliği")]
        public bool IsSupervisor { get; set; }
    }
}
