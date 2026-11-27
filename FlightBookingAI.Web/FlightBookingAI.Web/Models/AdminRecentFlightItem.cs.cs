using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlightBookingAI.Web.Models
{
    public class AdminRecentFlightItem
    {
        public int FlightID { get; set; }

        public string FlightNumber { get; set; }

        public string AirlineName { get; set; }

        public string DepartureCity { get; set; }

        public string ArrivalCity { get; set; }

        public DateTime DepartureTime { get; set; }

        public DateTime ArrivalTime { get; set; }

        public decimal Price { get; set; }

        public int TotalSeats { get; set; }

        public int AvailableSeats { get; set; }

        public string Status { get; set; }
    }
}