using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlightBookingAI.Web.Models
{
    public class AdminPaymentDetailViewModel
    {
        // Thông tin thanh toán
        public int PaymentID { get; set; }
        public int BookingID { get; set; }
        public string BookingCode { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public string TransactionCode { get; set; }
        public DateTime? PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; }

        // Thông tin đặt vé
        public DateTime BookingDate { get; set; }
        public int PassengerCount { get; set; }
        public decimal BookingTotalAmount { get; set; }
        public string BookingStatus { get; set; }

        // Thông tin khách hàng
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
    }
}