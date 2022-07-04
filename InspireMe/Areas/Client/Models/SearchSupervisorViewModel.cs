using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace InspireMe.Areas.Client.Models
{
    public class SearchSupervisorViewModel
    {

        [Required]
        [Display(Name = "İlgi Alanları")]
        public string fields { get; set; }
        
        public List<Tuple<IdentityUser, IEnumerable<string>>> Supervisors { get; set; }
    }
}
