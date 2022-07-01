using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace InspireMe.Models
{
    public class SearchSupervisorViewModel
    {

        [Required]
        [Display(Name = "İlgi Alanları")]
        public string fields { get; set; }
        
        public List<Tuple<IdentityUser, IEnumerable<string>>> supervisors { get; set; }
    }
}
