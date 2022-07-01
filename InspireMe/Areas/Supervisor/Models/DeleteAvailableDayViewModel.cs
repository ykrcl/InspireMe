using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace InspireMe.Models
{
    public class DeleteAvailableDayViewModel
    {

        [Required]
        public int Id { get; set; }
        
    }

}
