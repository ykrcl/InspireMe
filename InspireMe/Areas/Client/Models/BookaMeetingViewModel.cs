using System.ComponentModel.DataAnnotations;

namespace InspireMe.Models
{
    public class BookaMeetingViewModel
    {
        
      
        [Required]
        public string UserId { get; set; }
        [Display(Name = "Danışman Adı")]
        public string UserName { get; set; }
        [Display(Name = "Görüşme Tarihi")]
        [Required(ErrorMessage ="Bu alan gereklidir!")]
        public DateOnly Date { get; set; }

        [Display(Name = "Görüşme Saati")]
        [Required(ErrorMessage = "Bu alan gereklidir!")]
        [Range(0,23,ErrorMessage ="Saat 0 ile 24 arasında olmalıdır")]
        public int Hour { get; set; }



    }
}
