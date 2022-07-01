using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace InspireMe.Models
{
    public class CreateAvailableDayViewModel
    {

        [Required(ErrorMessage = "Bu alan gereklidir!")]
        [Display(Name = "Gün")]
        [Range(0,6,ErrorMessage ="Gün 0 ve 6 arasında olmalıdır.")]
        public int Day { get; set; }
        [Required(ErrorMessage = "Bu alan gereklidir!")]
        [Display(Name = "Saatler")]
        public List<int> Hours { get; set; }

        [Required(ErrorMessage = "Bu alan gereklidir!")]
        [Display(Name = "Fiyat")]
        public float Price
        {
            get;
            set;
        }
    }

}
