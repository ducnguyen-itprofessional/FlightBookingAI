
using FlightBookingAI.Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
namespace FlightBookingAI.Web.Controllers
{
    public class AdminFlightController : Controller
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

        // Kiểm tra đăng nhập Admin
        private bool IsAdmin()
        {
            return Session["AdminId"] != null &&
                   string.Equals(
                       Convert.ToString(Session["AdminRole"]),
                       "Admin",
                       StringComparison.OrdinalIgnoreCase);
        }
        // ==============================
        // DANH SÁCH + TÌM KIẾM + LỌC
        // ==============================

        [HttpGet]
        public ActionResult Index(string keyword, int? airlineID, string status)
        {
            // 1. Kiểm tra quyền Admin
            if (!IsAdmin())
            {
                return RedirectToAction("AdminLogin", "AdminAccount");
            }

            // 2. Khởi tạo ViewModel
            AdminFlightListViewModel model = new AdminFlightListViewModel
            {
                Flights = new List<AdminFlightItem>(),
                Keyword = keyword,
                AirlineID = airlineID,
                Status = status,
                Airlines = GetAirlineOptions()
            };

            // 3. Truy vấn danh sách chuyến bay
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                string sql = @"
            SELECT
                f.FlightID,
                f.FlightNumber,
                al.AirlineName,
                al.AirlineCode,
                dep.AirportCode AS DepartureAirportCode,
                dep.City AS DepartureCity,
                arr.AirportCode AS ArrivalAirportCode,
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
            WHERE
                (@Keyword IS NULL
                 OR f.FlightNumber LIKE '%' + @Keyword + '%'
                 OR al.AirlineName LIKE '%' + @Keyword + '%'
                 OR dep.City LIKE '%' + @Keyword + '%'
                 OR arr.City LIKE '%' + @Keyword + '%')
                AND (@AirlineID IS NULL OR f.AirlineID = @AirlineID)
                AND (@Status IS NULL OR f.Status = @Status)
            ORDER BY f.DepartureTime DESC";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@Keyword", SqlDbType.NVarChar, 200).Value =
                        string.IsNullOrWhiteSpace(keyword)
                        ? (object)DBNull.Value
                        : keyword.Trim();

                    command.Parameters.Add("@AirlineID", SqlDbType.Int).Value =
                        airlineID.HasValue
                        ? (object)airlineID.Value
                        : DBNull.Value;

                    command.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value =
                        string.IsNullOrWhiteSpace(status)
                        ? (object)DBNull.Value
                        : status.Trim();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.Flights.Add(new AdminFlightItem
                            {
                                FlightID = Convert.ToInt32(reader["FlightID"]),
                                FlightNumber = Convert.ToString(reader["FlightNumber"]),
                                AirlineName = Convert.ToString(reader["AirlineName"]),
                                AirlineCode = Convert.ToString(reader["AirlineCode"]),
                                DepartureAirportCode = Convert.ToString(reader["DepartureAirportCode"]),
                                DepartureCity = Convert.ToString(reader["DepartureCity"]),
                                ArrivalAirportCode = Convert.ToString(reader["ArrivalAirportCode"]),
                                ArrivalCity = Convert.ToString(reader["ArrivalCity"]),
                                DepartureTime = Convert.ToDateTime(reader["DepartureTime"]),
                                ArrivalTime = Convert.ToDateTime(reader["ArrivalTime"]),
                                Price = Convert.ToDecimal(reader["Price"]),
                                TotalSeats = Convert.ToInt32(reader["TotalSeats"]),
                                AvailableSeats = Convert.ToInt32(reader["AvailableSeats"]),
                                Status = Convert.ToString(reader["Status"])
                            });
                        }
                    }
                }
            }

            // 4. Trả về View danh sách
            return View("FlightIndex", model);
        }

        // ==============================
        // LẤY DANH SÁCH HÃNG HÀNG KHÔNG
        // ==============================
        private List<SelectListItem> GetAirlineOptions()
        {
            List<SelectListItem> options = new List<SelectListItem>();

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            {
                connection.Open();

                string sql = @"
                    SELECT AirlineID, AirlineName
                    FROM Airlines
                    ORDER BY AirlineName";

                using (SqlCommand command = new SqlCommand(sql, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        options.Add(new SelectListItem
                        {
                            Value = reader["AirlineID"].ToString(),
                            Text = reader["AirlineName"].ToString()
                        });
                    }
                }
            }

            return options;
        }

        // ==========================================
        // LẤY DANH SÁCH SÂN BAY
        // ==========================================
        private List<SelectListItem> GetAirportOptions()
        {
            List<SelectListItem> options = new List<SelectListItem>();

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                string sql = @"
            SELECT AirportID, AirportCode, AirportName, City
            FROM Airports
            ORDER BY City, AirportCode";

                using (SqlCommand command = new SqlCommand(sql, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        options.Add(new SelectListItem
                        {
                            Value = reader["AirportID"].ToString(),
                            Text = reader["AirportCode"] + " - " +
                                   reader["City"] + " (" +
                                   reader["AirportName"] + ")"
                        });
                    }
                }
            }

            return options;
        }

        // ==========================================
        // NẠP CÁC DANH SÁCH CHO FORM
        // ==========================================
        private void LoadFlightFormOptions(AdminFlightFormViewModel model)
        {
            model.AirlineOptions = GetAirlineOptions();
            model.AirportOptions = GetAirportOptions();

            model.StatusOptions = new List<SelectListItem>
    {
        new SelectListItem { Value = "Scheduled", Text = "Scheduled" },
        new SelectListItem { Value = "Delayed", Text = "Delayed" },
        new SelectListItem { Value = "Completed", Text = "Completed" },
        new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
    };
        }

        // ==========================================
        // THÊM CHUYẾN BAY - HIỂN THỊ FORM
        // ==========================================
        [HttpGet]
        public ActionResult Create()
        {
            if (!IsAdmin())
                return RedirectToAction("AdminLogin", "AdminAccount");

            AdminFlightFormViewModel model = new AdminFlightFormViewModel();

            model.DepartureTime = DateTime.Now.AddHours(1);
            model.ArrivalTime = DateTime.Now.AddHours(3);
            model.TotalSeats = 100;
            model.AvailableSeats = 100;
            model.Status = "Scheduled";

            LoadFlightFormOptions(model);

            return View("FlightCreate", model);
        }

        // ==========================================
        // THÊM CHUYẾN BAY - LƯU VÀO DATABASE
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AdminFlightFormViewModel model)
        {
            if (!IsAdmin())
                return RedirectToAction("AdminLogin", "AdminAccount");

            // Kiểm tra thời gian
            if (model.ArrivalTime <= model.DepartureTime)
            {
                ModelState.AddModelError(
                    "ArrivalTime",
                    "Thời gian đến phải sau thời gian khởi hành.");
            }

            // Kiểm tra sân bay
            if (model.DepartureAirportID == model.ArrivalAirportID)
            {
                ModelState.AddModelError(
                    "ArrivalAirportID",
                    "Sân bay đi và sân bay đến không được trùng nhau.");
            }

            // Kiểm tra số ghế
            if (model.AvailableSeats > model.TotalSeats)
            {
                ModelState.AddModelError(
                    "AvailableSeats",
                    "Số ghế còn trống không được lớn hơn tổng số ghế.");
            }

            if (model.TotalSeats <= 0 || model.AvailableSeats < 0)
            {
                ModelState.AddModelError(
                    "",
                    "Thông tin số ghế không hợp lệ.");
            }

            // Kiểm tra trạng thái hợp lệ
            string[] validStatuses =
            {
        "Scheduled", "Delayed", "Completed", "Cancelled"
    };

            if (!validStatuses.Contains(model.Status))
            {
                ModelState.AddModelError(
                    "Status",
                    "Trạng thái chuyến bay không hợp lệ.");
            }

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    // Kiểm tra hãng hàng không có tồn tại không
                    string checkAirline = @"
                SELECT COUNT(1)
                FROM Airlines
                WHERE AirlineID = @AirlineID";

                    using (SqlCommand command =
                        new SqlCommand(checkAirline, connection))
                    {
                        command.Parameters.Add("@AirlineID", SqlDbType.Int)
                            .Value = model.AirlineID;

                        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                        {
                            ModelState.AddModelError(
                                "AirlineID",
                                "Hãng hàng không không tồn tại.");
                        }
                    }

                    // Kiểm tra hai sân bay có tồn tại không
                    string checkAirport = @"
                SELECT COUNT(1)
                FROM Airports
                WHERE AirportID IN (@DepartureID, @ArrivalID)";

                    using (SqlCommand command =
                        new SqlCommand(checkAirport, connection))
                    {
                        command.Parameters.Add("@DepartureID", SqlDbType.Int)
                            .Value = model.DepartureAirportID;

                        command.Parameters.Add("@ArrivalID", SqlDbType.Int)
                            .Value = model.ArrivalAirportID;

                        if (Convert.ToInt32(command.ExecuteScalar()) != 2)
                        {
                            ModelState.AddModelError(
                                "",
                                "Sân bay đi hoặc sân bay đến không tồn tại.");
                        }
                    }

                    if (ModelState.IsValid)
                    {
                        string insertSql = @"
                    INSERT INTO Flights
                    (
                        FlightNumber,
                        AirlineID,
                        DepartureAirportID,
                        ArrivalAirportID,
                        DepartureTime,
                        ArrivalTime,
                        Price,
                        TotalSeats,
                        AvailableSeats,
                        Status
                    )
                    VALUES
                    (
                        @FlightNumber,
                        @AirlineID,
                        @DepartureAirportID,
                        @ArrivalAirportID,
                        @DepartureTime,
                        @ArrivalTime,
                        @Price,
                        @TotalSeats,
                        @AvailableSeats,
                        @Status
                    )";

                        using (SqlCommand command =
                            new SqlCommand(insertSql, connection))
                        {
                            command.Parameters.Add("@FlightNumber", SqlDbType.VarChar, 20)
                                .Value = model.FlightNumber.Trim();

                            command.Parameters.Add("@AirlineID", SqlDbType.Int)
                                .Value = model.AirlineID;

                            command.Parameters.Add("@DepartureAirportID", SqlDbType.Int)
                                .Value = model.DepartureAirportID;

                            command.Parameters.Add("@ArrivalAirportID", SqlDbType.Int)
                                .Value = model.ArrivalAirportID;

                            command.Parameters.Add("@DepartureTime", SqlDbType.DateTime2)
                                .Value = model.DepartureTime;

                            command.Parameters.Add("@ArrivalTime", SqlDbType.DateTime2)
                                .Value = model.ArrivalTime;

                            command.Parameters.Add("@Price", SqlDbType.Decimal)
                                .Value = model.Price;

                            command.Parameters.Add("@TotalSeats", SqlDbType.Int)
                                .Value = model.TotalSeats;

                            command.Parameters.Add("@AvailableSeats", SqlDbType.Int)
                                .Value = model.AvailableSeats;

                            command.Parameters.Add("@Status", SqlDbType.VarChar, 20)
                                .Value = model.Status;

                            command.ExecuteNonQuery();
                        }

                        TempData["FlightMessage"] =
                            "Thêm chuyến bay thành công!";

                        return RedirectToAction("Index");
                    }
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể thêm chuyến bay. Vui lòng kiểm tra dữ liệu và database.");
            }

            LoadFlightFormOptions(model);

            return View("FlightCreate", model);
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("AdminLogin", "AdminAccount");

            AdminFlightFormViewModel model = null;

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                string sql = @"
            SELECT
                FlightID,
                FlightNumber,
                AirlineID,
                DepartureAirportID,
                ArrivalAirportID,
                DepartureTime,
                ArrivalTime,
                Price,
                TotalSeats,
                AvailableSeats,
                Status
            FROM Flights
            WHERE FlightID = @FlightID";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@FlightID", SqlDbType.Int).Value = id;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model = new AdminFlightFormViewModel
                            {
                                FlightID = Convert.ToInt32(reader["FlightID"]),
                                FlightNumber = Convert.ToString(reader["FlightNumber"]),
                                AirlineID = Convert.ToInt32(reader["AirlineID"]),
                                DepartureAirportID = Convert.ToInt32(reader["DepartureAirportID"]),
                                ArrivalAirportID = Convert.ToInt32(reader["ArrivalAirportID"]),
                                DepartureTime = Convert.ToDateTime(reader["DepartureTime"]),
                                ArrivalTime = Convert.ToDateTime(reader["ArrivalTime"]),
                                Price = Convert.ToDecimal(reader["Price"]),
                                TotalSeats = Convert.ToInt32(reader["TotalSeats"]),
                                AvailableSeats = Convert.ToInt32(reader["AvailableSeats"]),
                                Status = Convert.ToString(reader["Status"])
                            };
                        }
                    }
                }
            }

            if (model == null)
                return HttpNotFound("Không tìm thấy chuyến bay.");

            LoadFlightFormOptions(model);

            return View("FlightEdit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(AdminFlightFormViewModel model)
        {
            if (!IsAdmin())
                return RedirectToAction("AdminLogin", "AdminAccount");

            if (model.FlightID <= 0)
                return HttpNotFound("Mã chuyến bay không hợp lệ.");

            // Kiểm tra thời gian
            if (model.ArrivalTime <= model.DepartureTime)
            {
                ModelState.AddModelError(
                    "ArrivalTime",
                    "Thời gian đến phải sau thời gian khởi hành.");
            }

            // Kiểm tra sân bay
            if (model.DepartureAirportID == model.ArrivalAirportID)
            {
                ModelState.AddModelError(
                    "ArrivalAirportID",
                    "Sân bay đi và sân bay đến không được trùng nhau.");
            }

            // Kiểm tra số ghế
            if (model.TotalSeats <= 0 ||
                model.AvailableSeats < 0 ||
                model.AvailableSeats > model.TotalSeats)
            {
                ModelState.AddModelError(
                    "",
                    "Thông tin số ghế không hợp lệ.");
            }

            if (model.Price < 0)
            {
                ModelState.AddModelError("Price", "Giá vé không hợp lệ.");
            }

            // Kiểm tra trạng thái
            string[] validStatuses =
            {
        "Scheduled", "Delayed", "Completed", "Cancelled"
    };

            if (!validStatuses.Contains(model.Status))
            {
                ModelState.AddModelError(
                    "Status",
                    "Trạng thái chuyến bay không hợp lệ.");
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    // Kiểm tra chuyến bay có tồn tại
                    string checkFlight = @"
                SELECT COUNT(1)
                FROM Flights
                WHERE FlightID = @FlightID";

                    using (SqlCommand command = new SqlCommand(checkFlight, connection))
                    {
                        command.Parameters.Add("@FlightID", SqlDbType.Int).Value =
                            model.FlightID;

                        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                            return HttpNotFound("Không tìm thấy chuyến bay.");
                    }

                    // Kiểm tra hãng hàng không
                    string checkAirline = @"
                SELECT COUNT(1)
                FROM Airlines
                WHERE AirlineID = @AirlineID";

                    using (SqlCommand command = new SqlCommand(checkAirline, connection))
                    {
                        command.Parameters.Add("@AirlineID", SqlDbType.Int).Value =
                            model.AirlineID;

                        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                        {
                            ModelState.AddModelError(
                                "AirlineID",
                                "Hãng hàng không không tồn tại.");
                        }
                    }

                    // Kiểm tra sân bay
                    string checkAirport = @"
                SELECT COUNT(1)
                FROM Airports
                WHERE AirportID IN (@DepartureID, @ArrivalID)";

                    using (SqlCommand command = new SqlCommand(checkAirport, connection))
                    {
                        command.Parameters.Add("@DepartureID", SqlDbType.Int).Value =
                            model.DepartureAirportID;

                        command.Parameters.Add("@ArrivalID", SqlDbType.Int).Value =
                            model.ArrivalAirportID;

                        if (Convert.ToInt32(command.ExecuteScalar()) != 2)
                        {
                            ModelState.AddModelError(
                                "",
                                "Sân bay đi hoặc sân bay đến không tồn tại.");
                        }
                    }

                    // Chỉ cập nhật khi dữ liệu hợp lệ
                    if (ModelState.IsValid)
                    {
                        string updateSql = @"
                    UPDATE Flights
                    SET
                        FlightNumber = @FlightNumber,
                        AirlineID = @AirlineID,
                        DepartureAirportID = @DepartureAirportID,
                        ArrivalAirportID = @ArrivalAirportID,
                        DepartureTime = @DepartureTime,
                        ArrivalTime = @ArrivalTime,
                        Price = @Price,
                        TotalSeats = @TotalSeats,
                        AvailableSeats = @AvailableSeats,
                        Status = @Status
                    WHERE FlightID = @FlightID";

                        using (SqlCommand command = new SqlCommand(updateSql, connection))
                        {
                            command.Parameters.Add("@FlightID", SqlDbType.Int).Value =
                                model.FlightID;

                            command.Parameters.Add("@FlightNumber", SqlDbType.VarChar, 20).Value =
                                model.FlightNumber.Trim();

                            command.Parameters.Add("@AirlineID", SqlDbType.Int).Value =
                                model.AirlineID;

                            command.Parameters.Add("@DepartureAirportID", SqlDbType.Int).Value =
                                model.DepartureAirportID;

                            command.Parameters.Add("@ArrivalAirportID", SqlDbType.Int).Value =
                                model.ArrivalAirportID;

                            command.Parameters.Add("@DepartureTime", SqlDbType.DateTime2).Value =
                                model.DepartureTime;

                            command.Parameters.Add("@ArrivalTime", SqlDbType.DateTime2).Value =
                                model.ArrivalTime;

                            command.Parameters.Add("@Price", SqlDbType.Decimal).Value =
                                model.Price;

                            command.Parameters.Add("@TotalSeats", SqlDbType.Int).Value =
                                model.TotalSeats;

                            command.Parameters.Add("@AvailableSeats", SqlDbType.Int).Value =
                                model.AvailableSeats;

                            command.Parameters.Add("@Status", SqlDbType.VarChar, 20).Value =
                                model.Status;

                            command.ExecuteNonQuery();
                        }

                        TempData["FlightMessage"] = "Cập nhật chuyến bay thành công!";

                        return RedirectToAction("Index");
                    }
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể cập nhật chuyến bay. Vui lòng kiểm tra dữ liệu và database.");
            }

            LoadFlightFormOptions(model);

            return View("FlightEdit", model);
        }

        // ==========================================
        // XÓA CHUYẾN BAY
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("AdminLogin", "AdminAccount");

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    string sql = @"
                DELETE FROM Flights
                WHERE FlightID = @FlightID
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM Bookings
                    WHERE Bookings.FlightID = Flights.FlightID
                )";

                    using (SqlCommand command =
                        new SqlCommand(sql, connection))
                    {
                        command.Parameters.Add("@FlightID", SqlDbType.Int)
                            .Value = id;

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            TempData["FlightMessage"] =
                                "Xóa chuyến bay thành công!";
                        }
                        else
                        {
                            TempData["FlightError"] =
                                "Không thể xóa chuyến bay: chuyến bay không tồn tại hoặc đã có đặt chỗ.";
                        }
                    }
                }
            }
            catch (Exception)
            {
                TempData["FlightError"] =
                    "Không thể xóa chuyến bay do dữ liệu đang được sử dụng hoặc có lỗi database.";
            }

            return RedirectToAction("Index");
        }


    }
}

