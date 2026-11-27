using FlightBookingAI.Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FlightBookingAI.Web.Controllers
{
    public class AdminBookingController : Controller
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

        [HttpGet]
        public ActionResult Index(string keyword, string bookingStatus, string paymentStatus)
        {
            if (!IsAdmin())
                return RedirectToAction("AdminLogin", "AdminAccount");

            AdminBookingListViewModel model = new AdminBookingListViewModel();

            model.Keyword = keyword;
            model.BookingStatus = bookingStatus;
            model.PaymentStatus = paymentStatus;

            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    string sql = @"
                SELECT
                    b.BookingID,
                    b.BookingCode,
                    b.BookingDate,
                    b.PassengerCount,
                    b.TotalAmount,
                    b.Status AS BookingStatus,

                    u.UserID,
                    u.FullName AS CustomerName,
                    u.Email AS CustomerEmail,
                    u.Phone AS CustomerPhone,

                    f.FlightID,
                    f.FlightNumber,
                    f.DepartureTime,
                    f.ArrivalTime,

                    p.PaymentMethod,
                    p.PaymentStatus

                FROM Bookings b
                INNER JOIN Users u ON b.UserID = u.UserID
                INNER JOIN Flights f ON b.FlightID = f.FlightID

                OUTER APPLY
                (
                    SELECT TOP 1
                        PaymentMethod,
                        PaymentStatus
                    FROM Payments
                    WHERE BookingID = b.BookingID
                    ORDER BY CreatedAt DESC, PaymentID DESC
                ) p

                WHERE
                    (
                        @Keyword IS NULL
                        OR b.BookingCode LIKE @Search
                        OR u.FullName LIKE @Search
                        OR u.Email LIKE @Search
                        OR u.Phone LIKE @Search
                        OR f.FlightNumber LIKE @Search
                    )
                    AND
                    (
                        @BookingStatus IS NULL
                        OR b.Status = @BookingStatus
                    )
                    AND
                    (
                        @PaymentStatus IS NULL
                        OR p.PaymentStatus = @PaymentStatus
                    )

                ORDER BY b.BookingDate DESC";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        string search = string.IsNullOrWhiteSpace(keyword)
                            ? null
                            : "%" + keyword.Trim() + "%";

                        command.Parameters.Add("@Keyword", SqlDbType.NVarChar, 200)
                            .Value = (object)search ?? DBNull.Value;

                        command.Parameters.Add("@Search", SqlDbType.NVarChar, 202)
                            .Value = (object)search ?? DBNull.Value;

                        command.Parameters.Add("@BookingStatus", SqlDbType.VarChar, 50)
                            .Value = string.IsNullOrWhiteSpace(bookingStatus)
                                ? (object)DBNull.Value
                                : bookingStatus;

                        command.Parameters.Add("@PaymentStatus", SqlDbType.VarChar, 50)
                            .Value = string.IsNullOrWhiteSpace(paymentStatus)
                                ? (object)DBNull.Value
                                : paymentStatus;

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                AdminBookingItem item = new AdminBookingItem();

                                item.BookingID = Convert.ToInt32(reader["BookingID"]);
                                item.BookingCode = Convert.ToString(reader["BookingCode"]);
                                item.BookingDate = Convert.ToDateTime(reader["BookingDate"]);
                                item.PassengerCount = Convert.ToInt32(reader["PassengerCount"]);
                                item.TotalAmount = Convert.ToDecimal(reader["TotalAmount"]);
                                item.BookingStatus = Convert.ToString(reader["BookingStatus"]);

                                item.UserID = Convert.ToInt32(reader["UserID"]);
                                item.CustomerName = Convert.ToString(reader["CustomerName"]);
                                item.CustomerEmail = Convert.ToString(reader["CustomerEmail"]);
                                item.CustomerPhone = reader["CustomerPhone"] == DBNull.Value
                                    ? ""
                                    : Convert.ToString(reader["CustomerPhone"]);

                                item.FlightID = Convert.ToInt32(reader["FlightID"]);
                                item.FlightNumber = Convert.ToString(reader["FlightNumber"]);
                                item.DepartureTime = Convert.ToDateTime(reader["DepartureTime"]);
                                item.ArrivalTime = Convert.ToDateTime(reader["ArrivalTime"]);

                                item.PaymentMethod = reader["PaymentMethod"] == DBNull.Value
                                    ? ""
                                    : Convert.ToString(reader["PaymentMethod"]);

                                item.PaymentStatus = reader["PaymentStatus"] == DBNull.Value
                                    ? "Chưa có thanh toán"
                                    : Convert.ToString(reader["PaymentStatus"]);

                                model.Bookings.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                TempData["FlightError"] = "Không thể tải danh sách đặt vé. Vui lòng kiểm tra database.";
            }

            return View("BookingIndex", model);
        }

        [HttpGet]
        public ActionResult Details(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("AdminLogin", "AdminAccount");

            AdminBookingDetailViewModel model = new AdminBookingDetailViewModel();

            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    // 1. Lấy thông tin đơn đặt vé
                    string sqlBooking = @"
                SELECT
                    b.BookingID,
                    b.BookingCode,
                    b.BookingDate,
                    b.PassengerCount,
                    b.TotalAmount,
                    b.Status AS BookingStatus,

                    u.UserID,
                    u.FullName AS CustomerName,
                    u.Email AS CustomerEmail,
                    u.Phone AS CustomerPhone,

                    f.FlightID,
                    f.FlightNumber,
                    f.DepartureTime,
                    f.ArrivalTime,

                    p.PaymentMethod,
                    p.PaymentStatus

                FROM Bookings b
                INNER JOIN Users u ON b.UserID = u.UserID
                INNER JOIN Flights f ON b.FlightID = f.FlightID

                OUTER APPLY
                (
                    SELECT TOP 1
                        PaymentMethod,
                        PaymentStatus
                    FROM Payments
                    WHERE BookingID = b.BookingID
                    ORDER BY CreatedAt DESC, PaymentID DESC
                ) p

                WHERE b.BookingID = @BookingID";

                    using (SqlCommand command = new SqlCommand(sqlBooking, connection))
                    {
                        command.Parameters.AddWithValue("@BookingID", id);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                TempData["BookingError"] = "Không tìm thấy đơn đặt vé.";
                                return RedirectToAction("Index");
                            }

                            model.Booking.BookingID = Convert.ToInt32(reader["BookingID"]);
                            model.Booking.BookingCode = Convert.ToString(reader["BookingCode"]);
                            model.Booking.BookingDate = Convert.ToDateTime(reader["BookingDate"]);
                            model.Booking.PassengerCount = Convert.ToInt32(reader["PassengerCount"]);
                            model.Booking.TotalAmount = Convert.ToDecimal(reader["TotalAmount"]);
                            model.Booking.BookingStatus = Convert.ToString(reader["BookingStatus"]);

                            model.Booking.UserID = Convert.ToInt32(reader["UserID"]);
                            model.Booking.CustomerName = Convert.ToString(reader["CustomerName"]);
                            model.Booking.CustomerEmail = Convert.ToString(reader["CustomerEmail"]);

                            model.Booking.CustomerPhone = reader["CustomerPhone"] == DBNull.Value
                                ? ""
                                : Convert.ToString(reader["CustomerPhone"]);

                            model.Booking.FlightID = Convert.ToInt32(reader["FlightID"]);
                            model.Booking.FlightNumber = Convert.ToString(reader["FlightNumber"]);
                            model.Booking.DepartureTime = Convert.ToDateTime(reader["DepartureTime"]);
                            model.Booking.ArrivalTime = Convert.ToDateTime(reader["ArrivalTime"]);

                            model.Booking.PaymentMethod = reader["PaymentMethod"] == DBNull.Value
                                ? ""
                                : Convert.ToString(reader["PaymentMethod"]);

                            model.Booking.PaymentStatus = reader["PaymentStatus"] == DBNull.Value
                                ? "Chưa có thanh toán"
                                : Convert.ToString(reader["PaymentStatus"]);
                        }
                    }

                    // 2. Lấy danh sách hành khách
                    string sqlPassengers = @"
                SELECT
                    BookingPassengerID,
                    FullName,
                    DateOfBirth,
                    Gender,
                    IdentityNumber,
                    PassengerType
                FROM BookingPassengers
                WHERE BookingID = @BookingID
                ORDER BY BookingPassengerID";

                    using (SqlCommand command = new SqlCommand(sqlPassengers, connection))
                    {
                        command.Parameters.AddWithValue("@BookingID", id);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                BookingPassengerItem passenger = new BookingPassengerItem();

                                passenger.BookingPassengerID = Convert.ToInt32(reader["BookingPassengerID"]);
                                passenger.FullName = Convert.ToString(reader["FullName"]);
                                passenger.DateOfBirth = Convert.ToDateTime(reader["DateOfBirth"]);

                                passenger.Gender = reader["Gender"] == DBNull.Value
                                    ? ""
                                    : Convert.ToString(reader["Gender"]);

                                passenger.IdentityNumber = reader["IdentityNumber"] == DBNull.Value
                                    ? ""
                                    : Convert.ToString(reader["IdentityNumber"]);

                                passenger.PassengerType = Convert.ToString(reader["PassengerType"]);

                                model.Passengers.Add(passenger);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                TempData["BookingError"] = "Không thể tải chi tiết đặt vé. Vui lòng kiểm tra database.";
                return RedirectToAction("Index");
            }

            return View("BookingDetails", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, string newStatus)
        {
            if (!IsAdmin())
                return RedirectToAction("AdminLogin", "AdminAccount");

            // Kiểm tra trạng thái được gửi lên
            if (newStatus != "Confirmed" &&
                newStatus != "Cancelled" &&
                newStatus != "Completed")
            {
                TempData["BookingError"] = "Trạng thái đặt vé không hợp lệ.";
                return RedirectToAction("Details", new { id = id });
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    // Lấy trạng thái hiện tại
                    string currentStatus;

                    string checkSql = @"
                SELECT Status
                FROM Bookings
                WHERE BookingID = @BookingID";

                    using (SqlCommand command = new SqlCommand(checkSql, connection))
                    {
                        command.Parameters.AddWithValue("@BookingID", id);

                        object result = command.ExecuteScalar();

                        if (result == null)
                        {
                            TempData["BookingError"] = "Không tìm thấy đơn đặt vé.";
                            return RedirectToAction("Index");
                        }

                        currentStatus = Convert.ToString(result);
                    }

                    // Kiểm tra quy tắc chuyển trạng thái
                    bool isValidTransition =
                        (currentStatus == "Pending" &&
                         (newStatus == "Confirmed" || newStatus == "Cancelled"))
                        ||
                        (currentStatus == "Confirmed" &&
                         (newStatus == "Completed" || newStatus == "Cancelled"));

                    if (!isValidTransition)
                    {
                        TempData["BookingError"] =
                            "Không thể chuyển từ trạng thái hiện tại sang trạng thái mới.";

                        return RedirectToAction("Details", new { id = id });
                    }

                    // Cập nhật trạng thái
                    string updateSql = @"
                UPDATE Bookings
                SET Status = @NewStatus
                WHERE BookingID = @BookingID
                  AND Status = @CurrentStatus";

                    using (SqlCommand command = new SqlCommand(updateSql, connection))
                    {
                        command.Parameters.AddWithValue("@NewStatus", newStatus);
                        command.Parameters.AddWithValue("@BookingID", id);
                        command.Parameters.AddWithValue("@CurrentStatus", currentStatus);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            TempData["BookingSuccess"] =
                                "Cập nhật trạng thái đặt vé thành công.";
                        }
                        else
                        {
                            TempData["BookingError"] =
                                "Đơn đặt vé đã thay đổi. Vui lòng tải lại trang.";
                        }
                    }
                }
            }
            catch (Exception)
            {
                TempData["BookingError"] =
                    "Không thể cập nhật trạng thái đặt vé. Vui lòng thử lại.";
            }

            return RedirectToAction("Details", new { id = id });
        }


    }
}