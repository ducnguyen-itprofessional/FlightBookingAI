
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace FlightBookingAI.Web.Models
{
    // Một chuyến bay trong danh sách quản lý
    public class AdminFlightItem
    {
        public int FlightID { get; set; }
        public string FlightNumber { get; set; }

        public string AirlineName { get; set; }
        public string AirlineCode { get; set; }

        public string DepartureAirportCode { get; set; }
        public string DepartureCity { get; set; }

        public string ArrivalAirportCode { get; set; }
        public string ArrivalCity { get; set; }

        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }

        public decimal Price { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public string Status { get; set; }
    }

    // Dữ liệu của trang danh sách chuyến bay
    public class AdminFlightListViewModel
    {
        public List<AdminFlightItem> Flights { get; set; }

        public string Keyword { get; set; }
        public int? AirlineID { get; set; }
        public string Status { get; set; }

        public List<SelectListItem> Airlines { get; set; }
    }

    // Dữ liệu dùng chung cho thêm và chỉnh sửa chuyến bay
    public class AdminFlightFormViewModel
    {
        public int FlightID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số hiệu chuyến bay.")]
        [StringLength(20, ErrorMessage = "Số hiệu chuyến bay tối đa 20 ký tự.")]
        [Display(Name = "Số hiệu chuyến bay")]
        public string FlightNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hãng hàng không.")]
        [Display(Name = "Hãng hàng không")]
        public int AirlineID { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn sân bay đi.")]
        [Display(Name = "Sân bay đi")]
        public int DepartureAirportID { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn sân bay đến.")]
        [Display(Name = "Sân bay đến")]
        public int ArrivalAirportID { get; set; }

        [Display(Name = "Thời gian khởi hành")]
        public DateTime DepartureTime { get; set; }

        [Display(Name = "Thời gian đến")]
        public DateTime ArrivalTime { get; set; }

        [Range(typeof(decimal), "0", "999999999",
            ErrorMessage = "Giá vé không hợp lệ.")]
        [Display(Name = "Giá vé")]
        public decimal Price { get; set; }

        [Range(1, 100000, ErrorMessage = "Tổng số ghế phải lớn hơn 0.")]
        [Display(Name = "Tổng số ghế")]
        public int TotalSeats { get; set; }

        [Range(0, 100000, ErrorMessage = "Số ghế còn trống không hợp lệ.")]
        [Display(Name = "Số ghế còn trống")]
        public int AvailableSeats { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
        [StringLength(20)]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; }

        public List<SelectListItem> AirlineOptions { get; set; }
        public List<SelectListItem> AirportOptions { get; set; }
        public List<SelectListItem> StatusOptions { get; set; }
    }
}
