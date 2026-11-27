using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlightBookingAI.Web.Models
{
    public class AdminReportViewModel
    {
        // Bộ lọc thời gian
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Tổng quan
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int TotalSuccessfulPayments { get; set; }
        public int TotalNewCustomers { get; set; }

        // Trạng thái đặt vé
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int CompletedBookings { get; set; }

        // Danh sách doanh thu theo tháng
        public List<AdminMonthlyRevenueItem> MonthlyRevenues
        { get; set; }

        // Danh sách phương thức thanh toán
        public List<AdminPaymentMethodItem> PaymentMethods
        { get; set; }

        public AdminReportViewModel()
        {
            MonthlyRevenues = new List<AdminMonthlyRevenueItem>();
            PaymentMethods = new List<AdminPaymentMethodItem>();
        }
    }

    public class AdminMonthlyRevenueItem
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Revenue { get; set; }
    }

    public class AdminPaymentMethodItem
    {
        public string PaymentMethod { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
