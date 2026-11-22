using FlightBookingAI.Web.Models;
using FlightBookingAI.Web.ViewModels;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Data.Entity;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace FlightBookingAI.Web.Controllers
{
    public class FlightsController : Controller
    {
        private FlightBookingAIEntities db = new FlightBookingAIEntities();
        // GET: Flights
        public ActionResult Index()
        {
            var flights = db.Flights.ToList();

            return View(flights);
        }

        [HttpGet]
        public ActionResult Search(string departureAirport,
                                   string arrivalAirport,
                                   DateTime? departureDate,
                                   int passengers = 1)
        {
            // Kiểm tra số hành khách hợp lệ
            if (passengers < 1)
            {
                passengers = 1;
            }

            // Khởi tạo truy vấn chuyến bay
            var flights = db.Flights.AsQueryable();

            // Tìm theo sân bay đi
            if (!string.IsNullOrWhiteSpace(departureAirport))
            {
                string keyword = departureAirport.Trim();

                flights = flights.Where(f =>
                    db.Airports.Any(a =>
                        a.AirportID == f.DepartureAirportID &&
                        (a.AirportCode.Contains(keyword) ||
                         a.AirportName.Contains(keyword) ||
                         a.City.Contains(keyword))));
            }

            // Tìm theo sân bay đến
            if (!string.IsNullOrWhiteSpace(arrivalAirport))
            {
                string keyword = arrivalAirport.Trim();

                flights = flights.Where(f =>
                    db.Airports.Any(a =>
                        a.AirportID == f.ArrivalAirportID &&
                        (a.AirportCode.Contains(keyword) ||
                         a.AirportName.Contains(keyword) ||
                         a.City.Contains(keyword))));
            }

            // Tìm theo ngày khởi hành
            if (departureDate.HasValue)
            {
                DateTime startDate = departureDate.Value.Date;
                DateTime endDate = startDate.AddDays(1);

                flights = flights.Where(f =>
                    f.DepartureTime >= startDate &&
                    f.DepartureTime < endDate);
            }

            // Chỉ hiển thị chuyến bay đủ ghế
            flights = flights.Where(f =>
                f.AvailableSeats >= passengers);

            // Sắp xếp theo thời gian khởi hành
            var result = flights
                .OrderBy(f => f.DepartureTime)
                .ToList();
            // Lưu số lượng hành khách để View kết quả sử dụng
            ViewBag.Passengers = passengers;

            return View("Index", result);
        }

        [HttpGet]
        public ActionResult Details(int id, int passengers = 1)
        {
            if (passengers < 1)
            {
                passengers = 1;
            }

            var flight = db.Flights.Find(id);

            if (flight == null)
            {
                return HttpNotFound();
            }

            ViewBag.Passengers = passengers;

            return View(flight);
        }


        [HttpGet]
        public ActionResult SeatSelection(int id, int passengers = 1)
        {
            if (passengers < 1)
            {
                passengers = 1;
            }

            var flight = db.Flights.Find(id);

            if (flight == null)
            {
                return HttpNotFound();
            }
            // Lấy danh sách ghế không thể đặt từ database
            var unavailableSeats = db.FlightSeats
                .Where(s => s.FlightID == id && s.Status != "Available")
                .Select(s => s.SeatNumber)
                .ToList();

            ViewBag.OccupiedSeats = unavailableSeats;

            // Kiểm tra đủ ghế cho số hành khách
            if (flight.AvailableSeats < passengers ||
                flight.Status != "Scheduled")
            {
                return RedirectToAction(
                    "Details",
                    new { id = id, passengers = passengers }
                );
            }

            ViewBag.Passengers = passengers;

            return View(flight);
        }

        [HttpGet]
        public ActionResult PassengerInformation(
            int id,
            int passengers = 1,
            string seats = "")
        {
            // Kiểm tra số lượng hành khách
            if (passengers < 1)
            {
                return new HttpStatusCodeResult(400,
                    "Số lượng hành khách không hợp lệ.");
            }

            // Lấy thông tin chuyến bay
            var flight = db.Flights.Find(id);

            if (flight == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra trạng thái và số ghế còn trống
            if (flight.Status != "Scheduled" ||
                flight.AvailableSeats < passengers)
            {
                return RedirectToAction("Details",
                    new { id = id, passengers = passengers });
            }

            // Chuyển chuỗi ghế thành danh sách
            var selectedSeats = (seats ?? "")
                .Split(new[] { ',' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToUpperInvariant())
                .ToList();

            // Kiểm tra phải chọn đủ số ghế
            if (selectedSeats.Count != passengers)
            {
                return RedirectToAction("SeatSelection",
                    new { id = id, passengers = passengers });
            }

            // Kiểm tra ghế không bị trùng
            if (selectedSeats.Distinct().Count() !=
                selectedSeats.Count)
            {
                return RedirectToAction("SeatSelection",
                    new { id = id, passengers = passengers });
            }

            // Kiểm tra mã ghế thuộc sơ đồ 30 hàng, A-F
            bool validSeats = selectedSeats.All(seat =>
            {
                if (seat.Length < 2)
                {
                    return false;
                }

                char letter = seat[seat.Length - 1];

                int row;

                return letter >= 'A' &&
                       letter <= 'F' &&
                       int.TryParse(
                           seat.Substring(0, seat.Length - 1),
                           out row) &&
                       row >= 1 &&
                       row <= 30;
            });

            if (!validSeats)
            {
                return RedirectToAction("SeatSelection",
                    new { id = id, passengers = passengers });
            }

            // Kiểm tra trạng thái ghế thực tế trong SQL Server
            var availableSeatCount = db.FlightSeats
                .Count(s => s.FlightID == id
                         && selectedSeats.Contains(s.SeatNumber)
                         && s.Status == "Available");

            if (availableSeatCount != selectedSeats.Count)
            {
                TempData["SeatError"] =
                    "Một hoặc nhiều ghế đã được đặt hoặc không còn khả dụng. Vui lòng chọn lại.";

                return RedirectToAction("SeatSelection",
                    new { id = id, passengers = passengers });
            }


            // Tạo ViewModel đặt vé
            var model = new PassengerBookingViewModel
            {
                FlightID = flight.FlightID,
                BookingRequestId = Guid.NewGuid(),
                FlightNumber = flight.FlightNumber,

                AirlineName = flight.Airline != null
                    ? flight.Airline.AirlineName
                    : "Hãng hàng không",

                DepartureAirport = flight.Airport1 != null
                    ? flight.Airport1.AirportCode
                    : "---",

                ArrivalAirport = flight.Airport != null
                    ? flight.Airport.AirportCode
                    : "---",

                PassengerCount = passengers,

                TotalAmount = flight.Price * passengers,

                Passengers = new List<PassengerInfoViewModel>()
            };

            // Tạo một biểu mẫu cho mỗi ghế
            foreach (string seat in selectedSeats)
            {
                model.Passengers.Add(
                    new PassengerInfoViewModel
                    {
                        SeatNumber = seat,
                        PassengerType = "Adult"
                    });
            }

            // Khôi phục thông tin khi quay lại từ trang xác nhận
            var pendingBooking = TempData.Peek("PendingBooking")
                as PassengerBookingViewModel;

            if (pendingBooking != null &&
                pendingBooking.FlightID == id &&
                pendingBooking.PassengerCount == passengers &&
                pendingBooking.Passengers != null &&
                pendingBooking.Passengers.Count == passengers)
            {
                var requestedSeats = seats.Split(',')
                    .Select(s => s.Trim())
                    .ToList();

                var savedSeats = pendingBooking.Passengers
                    .Select(p => p.SeatNumber)
                    .ToList();

                if (requestedSeats.SequenceEqual(savedSeats))
                {
                    return View(pendingBooking);
                }
            }

            return View(model);
        }

        private bool IsValidSeatNumber(string seatNumber)
        {
            if (string.IsNullOrWhiteSpace(seatNumber))
            {
                return false;
            }

            seatNumber = seatNumber.Trim().ToUpperInvariant();

            // Mã ghế phải có dạng: 1A, 12C, 30F...
            var match = System.Text.RegularExpressions.Regex.Match(
                seatNumber,
                @"^([1-9]|[12][0-9]|30)[A-F]$"
            );

            return match.Success;
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PassengerInformation(
            PassengerBookingViewModel model)
        {
            // Kiểm tra dữ liệu được gửi lên
            if (model == null)
            {
                return new HttpStatusCodeResult(400);
            }
            // Kiểm tra mã yêu cầu đặt vé
            if (model.BookingRequestId == Guid.Empty)
            {
                ModelState.AddModelError("",
                    "Phiên đặt vé không hợp lệ. Vui lòng chọn lại chuyến bay.");

                return View(model);
            }

            // Kiểm tra chuyến bay
            var flight = db.Flights.Find(model.FlightID);

            if (flight == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra trạng thái chuyến bay
            if (flight.Status != "Scheduled")
            {
                ModelState.AddModelError("",
                    "Chuyến bay hiện không khả dụng.");
            }

            // Kiểm tra số lượng hành khách
            if (model.Passengers == null ||
                model.PassengerCount < 1 ||
                model.Passengers.Count != model.PassengerCount)
            {
                ModelState.AddModelError("",
                    "Số lượng hành khách không hợp lệ.");
            }

            // Kiểm tra số ghế còn trống
            if (flight.AvailableSeats < model.PassengerCount)
            {
                ModelState.AddModelError("",
                    "Chuyến bay không còn đủ ghế.");
            }

            // Kiểm tra thông tin từng hành khách
            if (model.Passengers != null)
            {
                for (int i = 0; i < model.Passengers.Count; i++)
                {
                    var passenger = model.Passengers[i];

                    if (passenger.DateOfBirth.HasValue &&
                        passenger.DateOfBirth.Value.Date > DateTime.Today)
                    {
                        ModelState.AddModelError(
                            "Passengers[" + i + "].DateOfBirth",
                            "Ngày sinh không được lớn hơn ngày hiện tại.");
                    }

                    if (passenger.PassengerType != "Adult" &&
                        passenger.PassengerType != "Child")
                    {
                        ModelState.AddModelError(
                            "Passengers[" + i + "].PassengerType",
                            "Loại hành khách không hợp lệ.");
                    }
                }
            }

            // Không tin giá tiền hoặc thông tin chuyến bay từ trình duyệt
            model.FlightNumber = flight.FlightNumber;

            model.AirlineName = flight.Airline != null
                ? flight.Airline.AirlineName
                : "Hãng hàng không";

            model.DepartureAirport = flight.Airport1 != null
                ? flight.Airport1.AirportCode
                : "---";

            model.ArrivalAirport = flight.Airport != null
                ? flight.Airport.AirportCode
                : "---";

            model.TotalAmount = flight.Price * model.PassengerCount;

            // KIỂM TRA MÃ GHẾ CỦA HÀNH KHÁCH
            if (model.Passengers != null)
            {
                var selectedSeats = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

                for (int i = 0; i < model.Passengers.Count; i++)
                {
                    var passenger = model.Passengers[i];

                    if (passenger == null)
                    {
                        ModelState.AddModelError(
                            "",
                            "Thông tin hành khách không hợp lệ."
                        );

                        continue;
                    }

                    string seatNumber = passenger.SeatNumber;

                    // Kiểm tra định dạng mã ghế
                    if (!IsValidSeatNumber(seatNumber))
                    {
                        ModelState.AddModelError(
                            "Passengers[" + i + "].SeatNumber",
                            "Mã ghế không hợp lệ."
                        );

                        continue;
                    }

                    seatNumber = seatNumber.Trim().ToUpperInvariant();

                    // Kiểm tra ghế bị trùng
                    if (!selectedSeats.Add(seatNumber))
                    {
                        ModelState.AddModelError(
                            "Passengers[" + i + "].SeatNumber",
                            "Ghế " + seatNumber + " đã được chọn cho hành khách khác."
                        );
                    }
                }
            }

            // KIỂM TRA TRẠNG THÁI GHẾ TRONG DATABASE
            if (model.Passengers != null)
            {
                for (int i = 0; i < model.Passengers.Count; i++)
                {
                    var passenger = model.Passengers[i];

                    if (passenger == null ||
                        !IsValidSeatNumber(passenger.SeatNumber))
                    {
                        continue;
                    }

                    string seatNumber = passenger.SeatNumber
                        .Trim()
                        .ToUpperInvariant();

                    bool isAvailable = db.FlightSeats.Any(s =>
                        s.FlightID == model.FlightID &&
                        s.SeatNumber == seatNumber &&
                        s.Status == "Available");

                    if (!isAvailable)
                    {
                        ModelState.AddModelError(
                            "Passengers[" + i + "].SeatNumber",
                            "Ghế " + seatNumber +
                            " đã được đặt hoặc không còn khả dụng."
                        );
                    }
                }
            }

            // KIỂM TRA NGÀY SINH VÀ LOẠI HÀNH KHÁCH
            if (model.Passengers != null)
            {
                // Tạm thời tính tuổi theo ngày hiện tại.
                DateTime referenceDate = DateTime.Today;

                for (int i = 0; i < model.Passengers.Count; i++)
                {
                    var passenger = model.Passengers[i];

                    if (passenger == null)
                    {
                        continue;
                    }

                    string dateKey = "Passengers[" + i + "].DateOfBirth";
                    string typeKey = "Passengers[" + i + "].PassengerType";

                    if (!passenger.DateOfBirth.HasValue)
                    {
                        ModelState.AddModelError(
                            dateKey,
                            "Vui lòng chọn ngày sinh."
                        );

                        continue;
                    }

                    DateTime birthDate = passenger.DateOfBirth.Value.Date;

                    if (birthDate > referenceDate)
                    {
                        ModelState.AddModelError(
                            dateKey,
                            "Ngày sinh không được lớn hơn ngày hiện tại."
                        );

                        continue;
                    }

                    int age = referenceDate.Year - birthDate.Year;

                    if (birthDate > referenceDate.AddYears(-age))
                    {
                        age--;
                    }

                    string passengerType = passenger.PassengerType;

                    if (passengerType == "Adult")
                    {
                        if (age < 12)
                        {
                            ModelState.AddModelError(
                                typeKey,
                                "Người dưới 12 tuổi không được chọn loại Người lớn."
                            );
                        }
                    }
                    else if (passengerType == "Child")
                    {
                        if (age < 2 || age >= 12)
                        {
                            ModelState.AddModelError(
                                typeKey,
                                "Trẻ em phải từ đủ 2 đến dưới 12 tuổi."
                            );
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(
                            typeKey,
                            "Loại hành khách không hợp lệ."
                        );
                    }
                }
            }


            // Nếu dữ liệu chưa hợp lệ, hiển thị lại form
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Lưu tạm thông tin đặt vé sau khi Validation thành công
            TempData["PendingBooking"] = model;

            // Chuyển đến trang xác nhận đặt vé
            return RedirectToAction("BookingConfirmation");

        }

        [HttpGet]
        public ActionResult BookingConfirmation()
        {
            // Lấy thông tin đặt vé đang lưu tạm
            var model = TempData.Peek("PendingBooking")
                as PassengerBookingViewModel;

            // Nếu không có thông tin đặt vé
            if (model == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Hiển thị trang xác nhận
            return View(model);
        }

        // POST: Flights/ConfirmBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult ConfirmBooking(Guid bookingRequestId)
        {
            // 1. Xác định khách hàng đăng nhập
            string email = User.Identity.Name;

            var currentUser = db.Users
                .FirstOrDefault(u => u.Email == email);

            if (currentUser == null || currentUser.Role != "Customer")
            {
                return new HttpStatusCodeResult(403);
            }

            // 2. Lấy thông tin đặt vé đang chờ
            var model = TempData.Peek("PendingBooking")
                as PassengerBookingViewModel;

            // 2A. Kiểm tra mã yêu cầu được gửi từ biểu mẫu
            if (bookingRequestId == Guid.Empty)
            {
                TempData["BookingError"] =
                    "Mã yêu cầu đặt vé không hợp lệ.";

                return RedirectToAction("Index", "Flights");
            }

            // 2B. Nếu bản nháp không còn, kiểm tra Booking đã lưu
            if (model == null)
            {
                var previousBooking = db.Bookings
                    .AsNoTracking()
                    .FirstOrDefault(b =>
                        b.BookingRequestId == bookingRequestId &&
                        b.UserID == currentUser.UserID);

                if (previousBooking != null)
                {
                    TempData["BookingSuccess"] =
                        "Yêu cầu đặt vé đã được xử lý. Mã đặt vé: " +
                        previousBooking.BookingCode +
                        ". Trạng thái: " + previousBooking.Status + ".";

                    return RedirectToAction("Index", "Flights");
                }

                TempData["BookingError"] =
                    "Phiên đặt vé không còn hợp lệ.";

                return RedirectToAction("Index", "Flights");
            }

            // 2C. Đối chiếu mã POST với bản nháp phía server
            if (model.BookingRequestId != bookingRequestId)
            {
                TempData["BookingError"] =
                    "Mã yêu cầu không khớp với phiên đặt vé hiện tại.";

                return RedirectToAction("Index", "Flights");
            }
            // 3. Kiểm tra danh sách hành khách
            if (model.Passengers == null ||
                model.PassengerCount < 1 ||
                model.PassengerCount > 180 ||
                model.Passengers.Count != model.PassengerCount)
            {
                TempData["BookingError"] =
                    "Danh sách hành khách không hợp lệ.";

                return RedirectToAction("BookingConfirmation");
            }

            // 4. Kiểm tra dữ liệu từng hành khách
            var selectedSeats = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var passenger in model.Passengers)
            {
                if (passenger == null ||
                    string.IsNullOrWhiteSpace(passenger.FullName) ||
                    passenger.FullName.Trim().Length > 200 ||
                    !passenger.DateOfBirth.HasValue ||
                    passenger.DateOfBirth.Value.Date > DateTime.Today ||
                    string.IsNullOrWhiteSpace(passenger.PassengerType) ||
                    (passenger.PassengerType != "Adult" &&
                     passenger.PassengerType != "Child") ||
                    (passenger.IdentityNumber != null &&
                     passenger.IdentityNumber.Length > 30) ||
                    string.IsNullOrWhiteSpace(passenger.SeatNumber))
                {
                    TempData["BookingError"] =
                        "Thông tin hành khách không hợp lệ.";

                    return RedirectToAction("BookingConfirmation");
                }

                string seatNumber =
                    passenger.SeatNumber.Trim().ToUpperInvariant();

                if (!selectedSeats.Add(seatNumber))
                {
                    TempData["BookingError"] =
                        "Danh sách có ghế bị chọn trùng.";

                    return RedirectToAction("BookingConfirmation");
                }
            }

            // 5. Transaction bảo đảm tính toàn vẹn dữ liệu
            using (var transaction = db.Database.BeginTransaction(
                IsolationLevel.ReadCommitted))
            {
                try
                {
                    // 5A. Khóa xử lý theo từng BookingRequestId
                    string lockName = "FlightBookingAI:Booking:" +
                        model.BookingRequestId.ToString("N");

                    var lockResult = new SqlParameter(
                        "@LockResult", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };

                    var resourceParameter = new SqlParameter(
                        "@ResourceName", SqlDbType.NVarChar, 255)
                    {
                        Value = lockName
                    };

                    db.Database.ExecuteSqlCommand(
                        @"EXEC @LockResult = sys.sp_getapplock
                @Resource = @ResourceName,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 5000",
                        lockResult,
                        resourceParameter);

                    int result = (int)lockResult.Value;

                    if (result < 0)
                    {
                        transaction.Rollback();

                        TempData["BookingError"] =
                            "Hệ thống đang xử lý yêu cầu đặt vé. Vui lòng thử lại.";

                        return RedirectToAction("BookingConfirmation");
                    }

                    // 5B. Kiểm tra Booking đã tồn tại sau khi lấy khóa
                    var bookingAfterLock = db.Bookings
                        .AsNoTracking()
                        .FirstOrDefault(b =>
                            b.BookingRequestId == model.BookingRequestId);

                    if (bookingAfterLock != null)
                    {
                        transaction.Rollback();

                        if (bookingAfterLock.UserID != currentUser.UserID)
                        {
                            return new HttpStatusCodeResult(403);
                        }

                        TempData.Remove("PendingBooking");

                        TempData["BookingSuccess"] =
                            "Yêu cầu này đã được xử lý. Mã đặt vé: " +
                            bookingAfterLock.BookingCode +
                            ". Trạng thái: " + bookingAfterLock.Status + ".";

                        return RedirectToAction("Index", "Flights");
                    }

                    // 5C. Đọc lại chuyến bay từ database
                    var flight = db.Flights
                        .AsNoTracking()
                        .FirstOrDefault(f => f.FlightID == model.FlightID);

                    if (flight == null ||
                        flight.Status != "Scheduled" ||
                        flight.DepartureTime <= DateTime.Now ||
                        flight.AvailableSeats < model.PassengerCount)
                    {
                        transaction.Rollback();

                        TempData["BookingError"] =
                            "Chuyến bay không còn khả dụng hoặc không đủ ghế.";

                        return RedirectToAction("BookingConfirmation");
                    }

                    // 6. Giữ từng ghế bằng cập nhật có điều kiện
                    var reservedSeats = new List<FlightSeat>();

                    foreach (var passenger in model.Passengers)
                    {
                        string seatNumber =
                            passenger.SeatNumber.Trim().ToUpperInvariant();

                        var seat = db.FlightSeats
                            .AsNoTracking()
                            .FirstOrDefault(s =>
                                s.FlightID == model.FlightID &&
                                s.SeatNumber == seatNumber);

                        if (seat == null)
                        {
                            transaction.Rollback();

                            TempData["BookingError"] =
                                "Không tìm thấy ghế đã chọn.";

                            return RedirectToAction("BookingConfirmation");
                        }

                        int affected = db.Database.ExecuteSqlCommand(
                            @"UPDATE dbo.FlightSeats
                  SET Status = 'Held'
                  WHERE FlightSeatID = @p0
                    AND FlightID = @p1
                    AND Status = 'Available'",
                            seat.FlightSeatID,
                            model.FlightID);

                        if (affected != 1)
                        {
                            transaction.Rollback();

                            TempData["BookingError"] =
                                "Một ghế đã được người khác đặt. Vui lòng chọn lại.";

                            return RedirectToAction("BookingConfirmation");
                        }

                        reservedSeats.Add(seat);
                    }

                    // 7. Giảm số ghế còn trống
                    int updatedFlights = db.Database.ExecuteSqlCommand(
                        @"UPDATE dbo.Flights
              SET AvailableSeats = AvailableSeats - @p0
              WHERE FlightID = @p1
                AND AvailableSeats >= @p0
                AND Status = 'Scheduled'",
                        model.PassengerCount,
                        model.FlightID);

                    if (updatedFlights != 1)
                    {
                        transaction.Rollback();

                        TempData["BookingError"] =
                            "Chuyến bay không còn đủ ghế.";

                        return RedirectToAction("BookingConfirmation");
                    }

                    // 8. Tạo mã đặt vé
                    string bookingCode = "BK" +
                        Guid.NewGuid().ToString("N")
                            .Substring(0, 18).ToUpperInvariant();

                    // 9. Tạo Booking chờ thanh toán
                    var booking = new Booking
                    {
                        BookingCode = bookingCode,
                        BookingRequestId = model.BookingRequestId,
                        UserID = currentUser.UserID,
                        FlightID = model.FlightID,
                        BookingDate = DateTime.Now,
                        PassengerCount = model.PassengerCount,
                        TotalAmount = flight.Price * model.PassengerCount,
                        Status = "Pending"
                    };

                    db.Bookings.Add(booking);
                    db.SaveChanges();

                    // 10. Lưu danh sách hành khách
                    for (int i = 0; i < model.Passengers.Count; i++)
                    {
                        var passenger = model.Passengers[i];
                        var seat = reservedSeats[i];

                        var bookingPassenger = new BookingPassenger
                        {
                            BookingID = booking.BookingID,
                            FullName = passenger.FullName.Trim(),
                            DateOfBirth = passenger.DateOfBirth.Value.Date,
                            Gender = passenger.Gender,
                            IdentityNumber = passenger.IdentityNumber,
                            PassengerType = passenger.PassengerType,
                            FlightSeatID = seat.FlightSeatID
                        };

                        db.BookingPassengers.Add(bookingPassenger);
                    }

                    db.SaveChanges();

                    // 11. Xác nhận toàn bộ thay đổi
                    transaction.Commit();

                    // Xóa bản nháp đã xử lý thành công
                    TempData.Remove("PendingBooking");

                    TempData["BookingSuccess"] =
                        "Đã tạo đơn đặt vé " + bookingCode +
                        ". Vui lòng tiếp tục thanh toán.";

                    return RedirectToAction("Index", "Payment", new { bookingId = booking.BookingID });
                }
                catch (Exception)
{
    // Cố gắng hoàn tác transaction nếu chưa Commit
    try
    {
        transaction.Rollback();
    }
    catch (Exception)
    {
        // Transaction có thể đã Commit hoặc bị hủy
    }

    // Kiểm tra lại database bằng một DbContext mới
    // để xác định đơn đã thực sự được lưu hay chưa
    try
    {
        using (var checkDb = new FlightBookingAIEntities())
        {
            var existingBooking = checkDb.Bookings
                .AsNoTracking()
                .FirstOrDefault(b =>
                    b.BookingRequestId == bookingRequestId &&
                    b.UserID == currentUser.UserID);

            if (existingBooking != null)
            {
                TempData.Remove("PendingBooking");

                TempData["BookingSuccess"] =
                    "Yêu cầu đặt vé đã được xử lý. Mã đặt vé: " +
                    existingBooking.BookingCode +
                    ". Trạng thái: " + existingBooking.Status + ".";

                return RedirectToAction("Index", "Flights");
            }
        }
    }
    catch (Exception)
    {
        // Không thể kiểm tra trạng thái đơn lúc này
    }

    TempData["BookingError"] =
        "Chưa thể xác định kết quả đặt vé. " +
        "Vui lòng kiểm tra đơn trước khi thử lại.";

    return RedirectToAction("BookingConfirmation");
}
            }
        }
    
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}