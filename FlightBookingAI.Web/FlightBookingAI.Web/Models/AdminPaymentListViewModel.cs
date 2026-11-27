using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlightBookingAI.Web.Models
{
    public class AdminPaymentListViewModel
    {
        public List<AdminPaymentItem> Payments { get; set; }

        public string Keyword { get; set; }

        public string PaymentStatus { get; set; }

        public AdminPaymentListViewModel()
        {
            Payments = new List<AdminPaymentItem>();
        }
    }
}