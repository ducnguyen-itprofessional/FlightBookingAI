
using FlightBookingAI.Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace FlightBookingAI.Web.Controllers
{
    public class AdminAccountController : Controller
    {
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

        // LUỒNG 1: TRANG CHÍNH ADMIN
        [HttpGet]
        public ActionResult AdminIndex()
        {
            return View();
        }

        // LUỒNG 2: HIỂN THỊ TRANG ĐĂNG NHẬP
        [HttpGet]
        public ActionResult AdminLogin()
        {
            return View();
        }

        // LUỒNG 3: HIỂN THỊ TRANG ĐĂNG KÝ
        [HttpGet]
        public ActionResult AdminRegister()
        {
            return View();
        }

        // LUỒNG 4: XỬ LÝ ĐĂNG KÝ ADMIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminRegister(
            string fullName,
            string email,
            string phone,
            string password,
            string confirmPassword)
        {
            // 1. Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.RegisterMessage =
                    "Vui lòng điền đầy đủ thông tin.";
                return View();
            }

            // 2. Chuẩn hóa dữ liệu
            fullName = fullName.Trim();
            email = email.Trim().ToLowerInvariant();
            phone = phone.Trim();

            // 3. Kiểm tra email
            try
            {
                var mail = new System.Net.Mail.MailAddress(email);

                if (mail.Address != email)
                {
                    ViewBag.RegisterMessage =
                        "Địa chỉ email không hợp lệ.";
                    return View();
                }
            }
            catch
            {
                ViewBag.RegisterMessage =
                    "Địa chỉ email không hợp lệ.";
                return View();
            }

            // 4. Kiểm tra độ dài dữ liệu theo cấu trúc bảng
            if (fullName.Length > 100 ||
                email.Length > 150 ||
                phone.Length > 15)
            {
                ViewBag.RegisterMessage =
                    "Họ tên, email hoặc số điện thoại vượt quá độ dài cho phép.";
                return View();
            }

            // 5. Kiểm tra mật khẩu
            if (password.Length < 10)
            {
                ViewBag.RegisterMessage =
                    "Mật khẩu phải có ít nhất 10 ký tự.";
                return View();
            }

            // 6. Kiểm tra xác nhận mật khẩu
            if (password != confirmPassword)
            {
                ViewBag.RegisterMessage =
                    "Mật khẩu xác nhận không khớp.";
                return View();
            }

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    // 7. Kiểm tra email đã tồn tại
                    string checkSql = @"
                        SELECT COUNT(1)
                        FROM Admins
                        WHERE Email = @Email";

                    using (SqlCommand checkCommand =
                        new SqlCommand(checkSql, connection))
                    {
                        checkCommand.Parameters.Add(
                            "@Email", SqlDbType.NVarChar, 150
                        ).Value = email;

                        int count = Convert.ToInt32(
                            checkCommand.ExecuteScalar()
                        );

                        if (count > 0)
                        {
                            ViewBag.RegisterMessage =
                                "Email đã được sử dụng. Vui lòng chọn email khác.";

                            return View();
                        }
                    }

                    // 8. Mã hóa mật khẩu bằng BCrypt
                    string passwordHash =
                        BCrypt.Net.BCrypt.HashPassword(password);

                    // 9. Thêm tài khoản Admin vào SQL Server
                    string insertSql = @"
                        INSERT INTO Admins
                        (
                            FullName,
                            Email,
                            Phone,
                            PasswordHash,
                            Role,
                            IsActive,
                            CreatedAt
                        )
                        VALUES
                        (
                            @FullName,
                            @Email,
                            @Phone,
                            @PasswordHash,
                            @Role,
                            @IsActive,
                            @CreatedAt
                        )";

                    using (SqlCommand insertCommand =
                        new SqlCommand(insertSql, connection))
                    {
                        insertCommand.Parameters.Add(
                            "@FullName", SqlDbType.NVarChar, 100
                        ).Value = fullName;

                        insertCommand.Parameters.Add(
                            "@Email", SqlDbType.NVarChar, 150
                        ).Value = email;

                        insertCommand.Parameters.Add(
                            "@Phone", SqlDbType.VarChar, 15
                        ).Value = phone;

                        insertCommand.Parameters.Add(
                            "@PasswordHash", SqlDbType.NVarChar, 255
                        ).Value = passwordHash;

                        insertCommand.Parameters.Add(
                            "@Role", SqlDbType.NVarChar, 30
                        ).Value = "Admin";

                        insertCommand.Parameters.Add(
                            "@IsActive", SqlDbType.Bit
                        ).Value = true;

                        insertCommand.Parameters.Add(
                            "@CreatedAt", SqlDbType.DateTime2
                        ).Value = DateTime.Now;

                        insertCommand.ExecuteNonQuery();
                    }
                }

                ViewBag.RegisterMessage =
                    "Đăng ký Admin thành công! Bạn có thể đăng nhập.";

                ViewBag.RegisterSuccess = true;

                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);

                ViewBag.RegisterMessage =
                    "Đăng ký thất bại. Vui lòng kiểm tra kết nối cơ sở dữ liệu.";

                return View();
            }
        }

        // LUỒNG 5: XỬ LÝ ĐĂNG NHẬP ADMIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminLogin(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.LoginMessage =
                    "Vui lòng nhập email và mật khẩu.";

                return View();
            }

            email = email.Trim().ToLowerInvariant();

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    string sql = @"
                        SELECT
                            AdminId,
                            FullName,
                            Email,
                            PasswordHash,
                            Role,
                            IsActive
                        FROM Admins
                        WHERE Email = @Email";

                    using (SqlCommand command =
                        new SqlCommand(sql, connection))
                    {
                        command.Parameters.Add(
                            "@Email", SqlDbType.NVarChar, 150
                        ).Value = email;

                        using (SqlDataReader reader =
                            command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                ViewBag.LoginMessage =
                                    "Email hoặc mật khẩu không chính xác.";

                                return View();
                            }

                            int adminId = Convert.ToInt32(
                                reader["AdminId"]
                            );

                            string fullName =
                                reader["FullName"].ToString();

                            string passwordHash =
                                reader["PasswordHash"].ToString();

                            string role =
                                reader["Role"].ToString();

                            bool isActive =
                                Convert.ToBoolean(reader["IsActive"]);

                            // Kiểm tra tài khoản bị khóa
                            if (!isActive)
                            {
                                ViewBag.LoginMessage =
                                    "Tài khoản Admin đã bị vô hiệu hóa.";

                                return View();
                            }

                            // Chỉ cho phép tài khoản có quyền Admin
                            if (!string.Equals(
                                role,
                                "Admin",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                ViewBag.LoginMessage =
                                    "Tài khoản không có quyền Admin.";

                                return View();
                            }

                            // Xác minh mật khẩu BCrypt
                            bool passwordValid = false;

                            try
                            {
                                passwordValid =
                                    BCrypt.Net.BCrypt.Verify(
                                        password,
                                        passwordHash
                                    );
                            }
                            catch
                            {
                                passwordValid = false;
                            }

                            if (!passwordValid)
                            {
                                ViewBag.LoginMessage =
                                    "Email hoặc mật khẩu không chính xác.";

                                return View();
                            }

                            // Lưu thông tin đăng nhập vào Session
                            Session["AdminId"] = adminId;
                            Session["AdminName"] = fullName;
                            Session["AdminEmail"] = email;
                            Session["AdminRole"] = role;
                        }
                    }
                }

                return RedirectToAction("AdminDashboard");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);

                ViewBag.LoginMessage =
                    "Không thể đăng nhập. Vui lòng thử lại.";

                return View();
            }
        }

        // LUỒNG 6: DASHBOARD ADMIN
        [HttpGet]
        public ActionResult AdminDashboard()
        {
            ViewBag.SessionIsNewSession = Session.IsNewSession;

            // 1. Kiểm tra đăng nhập và quyền Admin
            if (Session["AdminId"] == null ||
                Session["AdminRole"] == null ||
                !string.Equals(
                    Session["AdminRole"].ToString(),
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("AdminLogin");
            }

            // 2. Lấy thông tin Admin
            ViewBag.AdminName = Session["AdminName"];
            ViewBag.AdminEmail = Session["AdminEmail"];
            ViewBag.SessionAdminId = Convert.ToString(Session["AdminId"]);
            ViewBag.SessionAdminRole = Convert.ToString(Session["AdminRole"]);

            // 3. Khởi tạo dữ liệu thống kê
            ViewBag.TotalFlights = 0;
            ViewBag.TotalBookings = 0;
            ViewBag.TotalCustomers = 0;
            ViewBag.TotalRevenue = 0m;
            // Thống kê trạng thái đặt vé
            ViewBag.PendingBookings = 0;
            ViewBag.ConfirmedBookings = 0;
            ViewBag.CancelledBookings = 0;
            ViewBag.CompletedBookings = 0;
            ViewBag.BookingStatusTotal = 0;

            // Mảng doanh thu 12 tháng, mặc định bằng 0
            decimal[] monthlyRevenue = new decimal[12];
            List<AdminRecentFlightItem> recentFlights =
                new List<AdminRecentFlightItem>();

            try
            {
                string connectionString =
                    ConfigurationManager.ConnectionStrings[
                        "FlightBookingAIConnection"
                    ].ConnectionString;

                using (SqlConnection conn =
                    new SqlConnection(connectionString))
                {
                    // Truy vấn 1: Thống kê tổng quan
                    // Truy vấn 2: Doanh thu từng tháng năm 2026

                    string query = @"

                        -- 1. Thống kê tổng quan
                        SELECT
                            (SELECT COUNT(*) FROM Flights)
                                AS TotalFlights,

                            (SELECT COUNT(*) FROM Bookings)
                                AS TotalBookings,

                            (SELECT COUNT(*) FROM Users
                             WHERE Role = 'Customer')
                                AS TotalCustomers,

                            (SELECT ISNULL(SUM(Amount), 0)
                             FROM Payments
                             WHERE PaymentStatus IN ('Success', 'Completed'))
                                AS TotalRevenue;

                        -- 2. Doanh thu theo từng tháng năm 2026
                        SELECT
                            MONTH(PaymentDate) AS RevenueMonth,
                            SUM(Amount) AS MonthlyRevenue
                        FROM Payments
                        WHERE PaymentStatus IN ('Success', 'Completed')
                          AND PaymentDate >= '2026-01-01'
                          AND PaymentDate < '2027-01-01'
                        GROUP BY MONTH(PaymentDate)
                        ORDER BY RevenueMonth;

                        -- 3. Thống kê trạng thái đặt vé
                        SELECT
                            Status,
                            COUNT(*) AS BookingCount
                        FROM Bookings
                        GROUP BY Status;
                    ";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            // 1. Đọc kết quả thống kê tổng quan
                            if (reader.Read())
                            {
                                ViewBag.TotalFlights =
                                    Convert.ToInt32(reader["TotalFlights"]);

                                ViewBag.TotalBookings =
                                    Convert.ToInt32(reader["TotalBookings"]);

                                ViewBag.TotalCustomers =
                                    Convert.ToInt32(reader["TotalCustomers"]);

                                ViewBag.TotalRevenue =
                                    Convert.ToDecimal(reader["TotalRevenue"]);
                            }

                            // 2. Đọc kết quả doanh thu theo tháng
                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    int month =
                                        Convert.ToInt32(reader["RevenueMonth"]);

                                    decimal revenue =
                                        Convert.ToDecimal(reader["MonthlyRevenue"]);

                                    if (month >= 1 && month <= 12)
                                    {
                                        monthlyRevenue[month - 1] = revenue;
                                    }
                                }
                            }

                            // 3. Đọc kết quả thống kê trạng thái đặt vé
                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    string status =
                                        Convert.ToString(reader["Status"]);

                                    int count =
                                        Convert.ToInt32(reader["BookingCount"]);

                                    switch (status)
                                    {
                                        case "Pending":
                                            ViewBag.PendingBookings = count;
                                            break;

                                        case "Confirmed":
                                            ViewBag.ConfirmedBookings = count;
                                            break;

                                        case "Cancelled":
                                            ViewBag.CancelledBookings = count;
                                            break;

                                        case "Completed":
                                            ViewBag.CompletedBookings = count;
                                            break;
                                    }
                                }
                            }

                            // 4. Tính tổng số lượt đặt vé thuộc 4 trạng thái
                            ViewBag.BookingStatusTotal =
                                Convert.ToInt32(ViewBag.PendingBookings) +
                                Convert.ToInt32(ViewBag.ConfirmedBookings) +
                                Convert.ToInt32(ViewBag.CancelledBookings) +
                                Convert.ToInt32(ViewBag.CompletedBookings);
                        }
                    }

                    // 5. Đưa dữ liệu doanh thu 12 tháng sang View
                    ViewBag.MonthlyRevenue = monthlyRevenue;

                    // 4. Lấy 4 chuyến bay gần đây
                    using (SqlConnection flightConn = new SqlConnection(connectionString))
                    {
                        string flightQuery = @"
        SELECT TOP 4
            f.FlightID,
            f.FlightNumber,
            al.AirlineName,
            dep.City AS DepartureCity,
            arr.City AS ArrivalCity,
            f.DepartureTime,
            f.ArrivalTime,
            f.Price,
            f.TotalSeats,
            f.AvailableSeats,
            f.Status
        FROM Flights f
        INNER JOIN Airlines al
            ON f.AirlineID = al.AirlineID
        INNER JOIN Airports dep
            ON f.DepartureAirportID = dep.AirportID
        INNER JOIN Airports arr
            ON f.ArrivalAirportID = arr.AirportID
        ORDER BY f.DepartureTime DESC";

                        using (SqlCommand flightCmd = new SqlCommand(flightQuery, flightConn))
                        {
                            flightConn.Open();

                            using (SqlDataReader flightReader = flightCmd.ExecuteReader())
                            {
                                while (flightReader.Read())
                                {
                                    AdminRecentFlightItem flight =
                                        new AdminRecentFlightItem();

                                    flight.FlightID =
                                        Convert.ToInt32(flightReader["FlightID"]);

                                    flight.FlightNumber =
                                        Convert.ToString(flightReader["FlightNumber"]);

                                    flight.AirlineName =
                                        Convert.ToString(flightReader["AirlineName"]);

                                    flight.DepartureCity =
                                        Convert.ToString(flightReader["DepartureCity"]);

                                    flight.ArrivalCity =
                                        Convert.ToString(flightReader["ArrivalCity"]);

                                    flight.DepartureTime =
                                        Convert.ToDateTime(flightReader["DepartureTime"]);

                                    flight.ArrivalTime =
                                        Convert.ToDateTime(flightReader["ArrivalTime"]);

                                    flight.Price =
                                        Convert.ToDecimal(flightReader["Price"]);

                                    flight.TotalSeats =
                                        Convert.ToInt32(flightReader["TotalSeats"]);

                                    flight.AvailableSeats =
                                        Convert.ToInt32(flightReader["AvailableSeats"]);

                                    flight.Status =
                                        Convert.ToString(flightReader["Status"]);

                                    recentFlights.Add(flight);
                                }
                            }
                        }
                    }

                    ViewBag.RecentFlights = recentFlights;

                    ViewBag.DashboardError = null;
                }
            }
            catch (Exception ex)
            {
                ViewBag.DashboardError =
                    "Không thể tải dữ liệu thống kê: " + ex.Message;

                ViewBag.TotalFlights = 0;
                ViewBag.TotalBookings = 0;
                ViewBag.TotalCustomers = 0;
                ViewBag.TotalRevenue = 0m;

                ViewBag.MonthlyRevenue = monthlyRevenue;
            }

            return View();
        }
        // LUỒNG 7: ĐĂNG XUẤT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminLogout()
        {
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("AdminLogin");
        }
    }
}
