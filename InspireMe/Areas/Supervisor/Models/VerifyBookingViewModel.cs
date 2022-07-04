using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace InspireMe.Areas.Supervisor.Models
{
    public class VerifyBookingViewModel
    {

        [Required]
        public Guid Id { get; set; }
        
    }

}
