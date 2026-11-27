using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlightBookingAI.Web.Models
{
    public class AdminCustomerListViewModel
    {
        public List<AdminCustomerItem> Customers { get; set; }

        public string Keyword { get; set; }

        public AdminCustomerListViewModel()
        {
            Customers = new List<AdminCustomerItem>();
        }
    }
}