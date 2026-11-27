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
    public class AdminPaymentController : Controller
    {
        private string ConnectionString
        {
            get
            {
                return ConfigurationManager
                    .ConnectionStrings["FlightBookingAIConnection"]
                    .ConnectionString;
            }
        }

        private bool IsAdmin()
        {
            return Session["AdminId"] != null &&
                   Convert.ToString(Session["AdminRole"]) == "Admin";
        }

        // GET: AdminPayment
        public ActionResult Index(string keyword, string paymentStatus)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "AdminAccount");
            }

            AdminPaymentListViewModel model =
                new AdminPaymentListViewModel();

            model.Keyword = keyword;
            model.PaymentStatus = paymentStatus;

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    string sql = @"
                        SELECT
                            p.PaymentID,
                            p.BookingID,
                            b.BookingCode,
                            p.Amount,
                            p.PaymentMethod,
                            p.PaymentStatus,
                            p.TransactionCode,
                            p.PaymentDate,
                            p.CreatedAt,
                            u.FullName AS CustomerName,
                            u.Email AS CustomerEmail
                        FROM Payments p
                        INNER JOIN Bookings b
                            ON p.BookingID = b.BookingID
                        INNER JOIN Users u
                            ON b.UserID = u.UserID
                        WHERE
                            (
                                @Keyword IS NULL
                                OR p.TransactionCode LIKE @SearchKeyword
                                OR b.BookingCode LIKE @SearchKeyword
                                OR u.FullName LIKE @SearchKeyword
                                OR u.Email LIKE @SearchKeyword
                            )
                            AND
                            (
                                @PaymentStatus IS NULL
                                OR p.PaymentStatus = @PaymentStatus
                            )
                        ORDER BY p.CreatedAt DESC, p.PaymentID DESC";

                    using (SqlCommand command =
                        new SqlCommand(sql, connection))
                    {
                        string searchKeyword =
                            string.IsNullOrWhiteSpace(keyword)
                            ? null
                            : "%" + keyword.Trim() + "%";

                        string selectedStatus =
                            string.IsNullOrWhiteSpace(paymentStatus)
                            ? null
                            : paymentStatus.Trim();

                        command.Parameters.AddWithValue(
                            "@Keyword",
                            (object)searchKeyword ?? DBNull.Value);

                        command.Parameters.AddWithValue(
                            "@SearchKeyword",
                            (object)searchKeyword ?? DBNull.Value);

                        command.Parameters.AddWithValue(
                            "@PaymentStatus",
                            (object)selectedStatus ?? DBNull.Value);

                        using (SqlDataReader reader =
                            command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                AdminPaymentItem item =
                                    new AdminPaymentItem();

                                item.PaymentID =
                                    Convert.ToInt32(reader["PaymentID"]);

                                item.BookingID =
                                    Convert.ToInt32(reader["BookingID"]);

                                item.BookingCode =
                                    Convert.ToString(reader["BookingCode"]);

                                item.Amount =
                                    Convert.ToDecimal(reader["Amount"]);

                                item.PaymentMethod =
                                    Convert.ToString(reader["PaymentMethod"]);

                                item.PaymentStatus =
                                    Convert.ToString(reader["PaymentStatus"]);

                                item.TransactionCode =
                                    reader["TransactionCode"] == DBNull.Value
                                    ? ""
                                    : Convert.ToString(reader["TransactionCode"]);

                                item.PaymentDate =
                                    reader["PaymentDate"] == DBNull.Value
                                    ? (DateTime?)null
                                    : Convert.ToDateTime(reader["PaymentDate"]);

                                item.CreatedAt =
                                    Convert.ToDateTime(reader["CreatedAt"]);

                                item.CustomerName =
                                    Convert.ToString(reader["CustomerName"]);

                                item.CustomerEmail =
                                    Convert.ToString(reader["CustomerEmail"]);

                                model.Payments.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                TempData["PaymentError"] =
                    "Không thể tải danh sách thanh toán. Vui lòng kiểm tra database.";
            }

            return View("PaymentIndex", model);
        }

        // GET: AdminPayment/Details/2

        public ActionResult Details(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "AdminAccount");
            }

            AdminPaymentDetailViewModel model = null;

            string sql = @"
        SELECT
            p.PaymentID,
            p.BookingID,
            b.BookingCode,
            p.Amount,
            p.PaymentMethod,
            p.PaymentStatus,
            p.TransactionCode,
            p.PaymentDate,
            p.CreatedAt,

            b.BookingDate,
            b.PassengerCount,
            b.TotalAmount AS BookingTotalAmount,
            b.Status AS BookingStatus,

            u.FullName AS CustomerName,
            u.Email AS CustomerEmail,
            u.Phone AS CustomerPhone

        FROM Payments p
        INNER JOIN Bookings b ON p.BookingID = b.BookingID
        INNER JOIN Users u ON b.UserID = u.UserID

        WHERE p.PaymentID = @PaymentID
    ";

            using (SqlConnection conn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@PaymentID", id);

                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model = new AdminPaymentDetailViewModel
                        {
                            PaymentID = Convert.ToInt32(reader["PaymentID"]),
                            BookingID = Convert.ToInt32(reader["BookingID"]),
                            BookingCode = reader["BookingCode"] == DBNull.Value
                                ? ""
                                : Convert.ToString(reader["BookingCode"]),

                            Amount = Convert.ToDecimal(reader["Amount"]),
                            PaymentMethod = reader["PaymentMethod"] == DBNull.Value
                                ? ""
                                : Convert.ToString(reader["PaymentMethod"]),

                            PaymentStatus = reader["PaymentStatus"] == DBNull.Value
                                ? ""
                                : Convert.ToString(reader["PaymentStatus"]),

                            TransactionCode = reader["TransactionCode"] == DBNull.Value
                                ? ""
                                : Convert.ToString(reader["TransactionCode"]),

                            PaymentDate = reader["PaymentDate"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(reader["PaymentDate"]),

                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            BookingDate = Convert.ToDateTime(reader["BookingDate"]),
                            PassengerCount = Convert.ToInt32(reader["PassengerCount"]),
                            BookingTotalAmount = Convert.ToDecimal(reader["BookingTotalAmount"]),

                            BookingStatus = reader["BookingStatus"] == DBNull.Value
                                ? ""
                                : Convert.ToString(reader["BookingStatus"]),

                            CustomerName = reader["CustomerName"] == DBNull.Value
                                ? ""
                                : Convert.ToString(reader["CustomerName"]),

                            CustomerEmail = reader["CustomerEmail"] == DBNull.Value
                                ? ""
                                : Convert.ToString(reader["CustomerEmail"]),

                            CustomerPhone = reader["CustomerPhone"] == DBNull.Value
                                ? ""
                                : Convert.ToString(reader["CustomerPhone"])
                        };
                    }
                }
            }

            if (model == null)
            {
                TempData["PaymentError"] = "Không tìm thấy giao dịch thanh toán.";
                return RedirectToAction("Index");
            }

            return View("PaymentTransactionDetails", model);
        }

    }
}