using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlightBookingAI.Web.Models
{
    public class AdminBookingItem
    {
        // Thông tin đặt vé
        public int BookingID { get; set; }
        public string BookingCode { get; set; }
        public DateTime BookingDate { get; set; }
        public int PassengerCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string BookingStatus { get; set; }

        // Thông tin khách hàng
        public int UserID { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }

        // Thông tin chuyến bay
        public int FlightID { get; set; }
        public string FlightNumber { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }

        // Thông tin thanh toán
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
    }
}