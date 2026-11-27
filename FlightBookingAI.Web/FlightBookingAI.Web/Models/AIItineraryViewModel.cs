
using System.Collections.Generic;

namespace FlightBookingAI.Web.Models
{
    public class AIItineraryViewModel
    {
        public string Destination { get; set; }

        public List<AIItineraryDayViewModel> Days { get; set; }
            = new List<AIItineraryDayViewModel>();
    }

    public class AIItineraryDayViewModel
    {
        public int Day { get; set; }

        public string Title { get; set; }

        public List<string> Places { get; set; }
            = new List<string>();

        public string Food { get; set; }
    }
}
