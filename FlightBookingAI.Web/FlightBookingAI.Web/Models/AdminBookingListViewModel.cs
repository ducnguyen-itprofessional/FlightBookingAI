using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlightBookingAI.Web.Models
{
    public class AdminBookingListViewModel
    {
        // Danh sách đặt vé
        public List<AdminBookingItem> Bookings { get; set; }

        // Điều kiện tìm kiếm
        public string Keyword { get; set; }

        // Lọc trạng thái đặt vé
        public string BookingStatus { get; set; }

        // Lọc trạng thái thanh toán
        public string PaymentStatus { get; set; }

        public AdminBookingListViewModel()
        {
            Bookings = new List<AdminBookingItem>();
        }
    }
}