namespace InspireMe.Areas.Meeting.Models
{
    public class MeetingParametersViewModel
    {
        public string MeetingId { get; set; }
        public string UserName { get; set; }

        public DateOnly Date { get; set; }
        public int Hour { get; set; }

        public string? ChatHistory { get; set; }
    }
}
