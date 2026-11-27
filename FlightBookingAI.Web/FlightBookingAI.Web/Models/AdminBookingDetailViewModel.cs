using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlightBookingAI.Web.Models
{
    public class AdminBookingDetailViewModel
    {
        // Thông tin đặt vé
        public AdminBookingItem Booking { get; set; }

        // Danh sách hành khách
        public List<BookingPassengerItem> Passengers { get; set; }

        public AdminBookingDetailViewModel()
        {
            Booking = new AdminBookingItem();
            Passengers = new List<BookingPassengerItem>();
        }
    }

    // Thông tin từng hành khách
    public class BookingPassengerItem
    {
        public int BookingPassengerID { get; set; }
        public string FullName { get; set; }
        public System.DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string IdentityNumber { get; set; }
        public string PassengerType { get; set; }
    }
}
