using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace InspireMe.Areas.Supervisor.Models
{
    public class DeleteAvailableDayViewModel
    {

        [Required]
        public int Id { get; set; }
        
    }

}
