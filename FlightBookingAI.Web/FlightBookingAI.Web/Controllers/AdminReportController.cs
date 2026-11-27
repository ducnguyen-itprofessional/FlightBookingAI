using FlightBookingAI.Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FlightBookingAI.Web.Controllers
{
    public class AdminReportController : Controller
    {
        // GET: AdminReport
        // Lấy chuỗi kết nối SQL Server
        private string ConnectionString
        {
            get
            {
                return ConfigurationManager
                    .ConnectionStrings["FlightBookingAIConnection"]
                    .ConnectionString;
            }
        }

        // Kiểm tra quyền Admin
        private bool IsAdmin()
        {
            return Session["AdminId"] != null &&
                   Convert.ToString(Session["AdminRole"]) == "Admin";
        }

        // Trang báo cáo và thống kê
        public ActionResult Index(DateTime? fromDate, DateTime? toDate)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "AdminAccount");
            }

            // Mặc định: từ đầu năm đến ngày hiện tại
            DateTime startDate = fromDate ??
                new DateTime(DateTime.Today.Year, 1, 1);

            DateTime endDate = toDate ?? DateTime.Today;

            // Kiểm tra khoảng thời gian
            if (startDate > endDate)
            {
                DateTime temp = startDate;
                startDate = endDate;
                endDate = temp;
            }

            DateTime endExclusive = endDate.AddDays(1);

            AdminReportViewModel model = new AdminReportViewModel();

            model.FromDate = startDate;
            model.ToDate = endDate;

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(ConnectionString))
                {
                    string query = @"

                        -- 1. Thống kê tổng quan
                        SELECT
                            (
                                SELECT ISNULL(SUM(Amount), 0)
                                FROM Payments
                                WHERE PaymentStatus IN ('Success', 'Completed')
                                  AND PaymentDate >= @FromDate
                                  AND PaymentDate < @ToDate
                            ) AS TotalRevenue,

                            (
                                SELECT COUNT(*)
                                FROM Bookings
                                WHERE BookingDate >= @FromDate
                                  AND BookingDate < @ToDate
                            ) AS TotalBookings,

                            (
                                SELECT COUNT(*)
                                FROM Payments
                                WHERE PaymentStatus = 'Success'
                                  AND PaymentDate >= @FromDate
                                  AND PaymentDate < @ToDate
                            ) AS TotalSuccessfulPayments,

                            (
                                SELECT COUNT(*)
                                FROM Users
                                WHERE Role = 'Customer'
                                  AND CreatedAt >= @FromDate
                                  AND CreatedAt < @ToDate
                            ) AS TotalNewCustomers;


                        -- 2. Thống kê trạng thái đặt vé
                        SELECT Status, COUNT(*) AS BookingCount
                        FROM Bookings
                        WHERE BookingDate >= @FromDate
                          AND BookingDate < @ToDate
                        GROUP BY Status;


                        -- 3. Doanh thu theo tháng
                        SELECT
                            YEAR(PaymentDate) AS RevenueYear,
                            MONTH(PaymentDate) AS RevenueMonth,
                            SUM(Amount) AS MonthlyRevenue
                        FROM Payments
                        WHERE PaymentStatus IN ('Success', 'Completed')
                          AND PaymentDate >= @FromDate
                          AND PaymentDate < @ToDate
                        GROUP BY
                            YEAR(PaymentDate),
                            MONTH(PaymentDate)
                        ORDER BY
                            RevenueYear,
                            RevenueMonth;


                        -- 4. Thống kê phương thức thanh toán
                        SELECT
                            PaymentMethod,
                            COUNT(*) AS TransactionCount,
                            SUM(
                                CASE
                                    WHEN PaymentStatus IN ('Success', 'Completed')
                                    THEN Amount
                                    ELSE 0
                                END
                            ) AS TotalAmount
                        FROM Payments
                        WHERE PaymentDate >= @FromDate
                          AND PaymentDate < @ToDate
                        GROUP BY PaymentMethod
                        ORDER BY TransactionCount DESC;
                    ";

                    using (SqlCommand cmd =
                        new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue(
                            "@FromDate", startDate);

                        cmd.Parameters.AddWithValue(
                            "@ToDate", endExclusive);

                        conn.Open();

                        using (SqlDataReader reader =
                            cmd.ExecuteReader())
                        {
                            // 1. Đọc tổng quan
                            if (reader.Read())
                            {
                                model.TotalRevenue =
                                    Convert.ToDecimal(reader["TotalRevenue"]);

                                model.TotalBookings =
                                    Convert.ToInt32(reader["TotalBookings"]);

                                model.TotalSuccessfulPayments =
                                    Convert.ToInt32(reader["TotalSuccessfulPayments"]);

                                model.TotalNewCustomers =
                                    Convert.ToInt32(reader["TotalNewCustomers"]);
                            }

                            // 2. Đọc trạng thái đặt vé
                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    string status =
                                        Convert.ToString(reader["Status"]).Trim();

                                    int count =
                                        Convert.ToInt32(reader["BookingCount"]);

                                    if (status.Equals(
                                        "Pending",
                                        StringComparison.OrdinalIgnoreCase))
                                    {
                                        model.PendingBookings = count;
                                    }
                                    else if (status.Equals(
                                        "Confirmed",
                                        StringComparison.OrdinalIgnoreCase))
                                    {
                                        model.ConfirmedBookings = count;
                                    }
                                    else if (status.Equals(
                                        "Cancelled",
                                        StringComparison.OrdinalIgnoreCase))
                                    {
                                        model.CancelledBookings = count;
                                    }
                                    else if (status.Equals(
                                        "Completed",
                                        StringComparison.OrdinalIgnoreCase))
                                    {
                                        model.CompletedBookings = count;
                                    }
                                }
                            }

                            // 3. Đọc doanh thu theo tháng
                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    AdminMonthlyRevenueItem item =
                                        new AdminMonthlyRevenueItem();

                                    item.Year =
                                        Convert.ToInt32(reader["RevenueYear"]);

                                    item.Month =
                                        Convert.ToInt32(reader["RevenueMonth"]);

                                    item.Revenue =
                                        Convert.ToDecimal(reader["MonthlyRevenue"]);

                                    model.MonthlyRevenues.Add(item);
                                }
                            }

                            // 4. Đọc phương thức thanh toán
                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    AdminPaymentMethodItem item =
                                        new AdminPaymentMethodItem();

                                    item.PaymentMethod =
                                        Convert.ToString(reader["PaymentMethod"]);

                                    item.TransactionCount =
                                        Convert.ToInt32(reader["TransactionCount"]);

                                    item.TotalAmount =
                                        Convert.ToDecimal(reader["TotalAmount"]);

                                    model.PaymentMethods.Add(item);
                                }
                            }
                        }
                    }
                }

                ViewBag.ReportError = null;
            }
            catch (Exception ex)
            {
                ViewBag.ReportError =
                    "Không thể tải dữ liệu báo cáo: " + ex.Message;
            }

            return View("ReportIndex", model);
        }

        // Trang chi tiết doanh thu
        public ActionResult RevenueDetails(
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "AdminLogin",
                    "AdminAccount");
            }

            DateTime startDate = fromDate ??
                new DateTime(DateTime.Today.Year, 1, 1);

            DateTime endDate = toDate ?? DateTime.Today;

            if (startDate > endDate)
            {
                DateTime temp = startDate;
                startDate = endDate;
                endDate = temp;
            }

            DateTime endExclusive = endDate.AddDays(1);

            AdminRevenueReportViewModel model =
                new AdminRevenueReportViewModel();

            model.FromDate = startDate;
            model.ToDate = endDate;

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(ConnectionString))
                {
                    string query = @"

                        -- 1. Thống kê doanh thu
                        SELECT
                            ISNULL(SUM(
                                CASE
                                    WHEN PaymentStatus IN ('Success', 'Completed')
                                    THEN Amount ELSE 0
                                END
                            ), 0) AS TotalRevenue,

                            ISNULL(SUM(CASE
                                WHEN PaymentStatus IN ('Success', 'Completed')
                                THEN 1 ELSE 0
                            END), 0) AS SuccessfulPayments,

                            ISNULL(SUM(CASE
                                WHEN PaymentStatus = 'Failed'
                                THEN 1 ELSE 0
                            END), 0) AS FailedPayments,
                            ISNULL(SUM(CASE
                                WHEN PaymentStatus = 'Refunded'
                                THEN Amount ELSE 0
                            END), 0) AS RefundedAmount

                        FROM Payments
                        WHERE PaymentDate >= @FromDate
                          AND PaymentDate < @ToDate;


                        -- 2. Danh sách giao dịch
                        SELECT
                            P.PaymentID,
                            B.BookingCode,
                            U.FullName AS CustomerName,
                            U.Email,
                            P.PaymentMethod,
                            P.PaymentStatus,
                            P.TransactionCode,
                            P.Amount,
                            P.PaymentDate

                        FROM Payments P

                        INNER JOIN Bookings B
                            ON P.BookingID = B.BookingID

                        INNER JOIN Users U
                            ON B.UserID = U.UserID

                        WHERE
                            P.PaymentDate >= @FromDate
                            AND P.PaymentDate < @ToDate

                        ORDER BY P.PaymentDate DESC;


                        -- 3. Doanh thu theo tháng
                        SELECT
                            YEAR(PaymentDate) AS RevenueYear,
                            MONTH(PaymentDate) AS RevenueMonth,
                            SUM(Amount) AS MonthlyRevenue

                        FROM Payments

                        WHERE PaymentStatus IN ('Success', 'Completed')
                          AND PaymentDate >= @FromDate
                          AND PaymentDate < @ToDate

                        GROUP BY
                            YEAR(PaymentDate),
                            MONTH(PaymentDate)

                        ORDER BY
                            RevenueYear,
                            RevenueMonth;
                    ";

                    using (SqlCommand cmd =
                        new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue(
                            "@FromDate", startDate);

                        cmd.Parameters.AddWithValue(
                            "@ToDate", endExclusive);

                        conn.Open();

                        using (SqlDataReader reader =
                            cmd.ExecuteReader())
                        {
                            // 1. Đọc thống kê doanh thu
                            if (reader.Read())
                            {
                                model.TotalRevenue =
                                    Convert.ToDecimal(
                                        reader["TotalRevenue"]);

                                model.SuccessfulPayments =
                                    Convert.ToInt32(
                                        reader["SuccessfulPayments"]);

                                model.FailedPayments =
                                    Convert.ToInt32(
                                        reader["FailedPayments"]);

                                model.RefundedAmount =
                                    Convert.ToDecimal(
                                        reader["RefundedAmount"]);
                            }

                            // 2. Đọc danh sách giao dịch
                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    AdminRevenueTransactionItem item =
                                        new AdminRevenueTransactionItem();

                                    item.PaymentID =
                                        Convert.ToInt32(
                                            reader["PaymentID"]);

                                    item.BookingCode =
                                        Convert.ToString(
                                            reader["BookingCode"]);

                                    item.CustomerName =
                                        Convert.ToString(
                                            reader["CustomerName"]);

                                    item.Email =
                                        Convert.ToString(
                                            reader["Email"]);

                                    item.PaymentMethod =
                                        Convert.ToString(
                                            reader["PaymentMethod"]);

                                    item.PaymentStatus =
                                        Convert.ToString(
                                            reader["PaymentStatus"]);

                                    item.TransactionCode =
                                        reader["TransactionCode"] == DBNull.Value
                                        ? ""
                                        : Convert.ToString(
                                            reader["TransactionCode"]);

                                    item.Amount =
                                        Convert.ToDecimal(
                                            reader["Amount"]);

                                    item.PaymentDate =
                                        reader["PaymentDate"] == DBNull.Value
                                        ? (DateTime?)null
                                        : Convert.ToDateTime(
                                            reader["PaymentDate"]);

                                    model.Transactions.Add(item);
                                }
                            }

                            // 3. Đọc doanh thu theo tháng
                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    AdminMonthlyRevenueItem item =
                                        new AdminMonthlyRevenueItem();

                                    item.Year =
                                        Convert.ToInt32(
                                            reader["RevenueYear"]);

                                    item.Month =
                                        Convert.ToInt32(
                                            reader["RevenueMonth"]);

                                    item.Revenue =
                                        Convert.ToDecimal(
                                            reader["MonthlyRevenue"]);

                                    model.MonthlyRevenues.Add(item);
                                }
                            }
                        }
                    }
                }

                ViewBag.ReportError = null;
            }
            catch (Exception ex)
            {
                ViewBag.ReportError =
                    "Không thể tải chi tiết doanh thu: " + ex.Message;
            }

            return View("RevenueDetails", model);
        }

    }
}