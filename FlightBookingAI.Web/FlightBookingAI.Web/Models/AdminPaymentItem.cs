using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlightBookingAI.Web.Models
{
    public class AdminPaymentItem
    {
        public int PaymentID { get; set; }

        public int BookingID { get; set; }

        public string BookingCode { get; set; }

        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }

        public string TransactionCode { get; set; }

        public DateTime? PaymentDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public string CustomerName { get; set; }

        public string CustomerEmail { get; set; }
    }
}