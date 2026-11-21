
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlightBookingAI.Web.ViewModels
{
    public class PassengerBookingViewModel
    {
        // Chuyến bay được chọn
        public int FlightID { get; set; }
        // Mã duy nhất của một yêu cầu đặt vé
        public Guid BookingRequestId { get; set; }
        // Thông tin hiển thị chuyến bay
        public string FlightNumber { get; set; }

        public string AirlineName { get; set; }

        public string DepartureAirport { get; set; }

        public string ArrivalAirport { get; set; }

        // Số lượng hành khách
        [Range(1, 180, ErrorMessage = "Số hành khách không hợp lệ.")]
        public int PassengerCount { get; set; }

        // Danh sách thông tin hành khách
        public List<PassengerInfoViewModel> Passengers { get; set; }

        public PassengerBookingViewModel()
        {
            Passengers = new List<PassengerInfoViewModel>();
        }

        // Tổng tiền dự kiến
        public decimal TotalAmount { get; set; }
    }
}
