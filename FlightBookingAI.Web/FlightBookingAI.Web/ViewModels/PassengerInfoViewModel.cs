
using System;
using System.ComponentModel.DataAnnotations;

namespace FlightBookingAI.Web.ViewModels
{
    public class PassengerInfoViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(200)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày sinh.")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Giới tính")]
        public string Gender { get; set; }

        [StringLength(30)]
        [Display(Name = "CCCD / Hộ chiếu")]
        public string IdentityNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại hành khách.")]
        [Display(Name = "Loại hành khách")]
        public string PassengerType { get; set; }

        // Mã ghế đã chọn ở bước trước
        public string SeatNumber { get; set; }
    }
}
