using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlightBookingAI.Web.Models
{
    public class AdminRevenueReportViewModel
    {
        // Khoảng thời gian thống kê
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        // Các chỉ số doanh thu
        public decimal TotalRevenue { get; set; }

        public int SuccessfulPayments { get; set; }

        public int FailedPayments { get; set; }

        public decimal RefundedAmount { get; set; }

        // Danh sách giao dịch
        public List<AdminRevenueTransactionItem> Transactions
        {
            get;
            set;
        }

        // Doanh thu theo tháng
        public List<AdminMonthlyRevenueItem> MonthlyRevenues
        {
            get;
            set;
        }

        // Khởi tạo danh sách
        public AdminRevenueReportViewModel()
        {
            Transactions =
                new List<AdminRevenueTransactionItem>();

            MonthlyRevenues =
                new List<AdminMonthlyRevenueItem>();
        }
    }

    // Thông tin từng giao dịch thanh toán
    public class AdminRevenueTransactionItem
    {
        public int PaymentID { get; set; }

        public string BookingCode { get; set; }

        public string CustomerName { get; set; }

        public string Email { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }

        public string TransactionCode { get; set; }

        public decimal Amount { get; set; }

        public DateTime? PaymentDate { get; set; }
    }
}
