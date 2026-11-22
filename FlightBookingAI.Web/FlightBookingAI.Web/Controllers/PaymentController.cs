
using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using FlightBookingAI.Web.Models;

namespace FlightBookingAI.Web.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly FlightBookingAIEntities db =
            new FlightBookingAIEntities();

        // GET: Payment/Index?bookingId=4
        [HttpGet]
        public ActionResult Index(int bookingId)
        {
            // 1. Xác định người dùng đang đăng nhập
            string email = User.Identity.Name;

            var currentUser = db.Users
                .FirstOrDefault(u => u.Email == email);

            if (currentUser == null ||
                currentUser.Role != "Customer")
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden);
            }

            // 2. Lấy Booking thuộc về người dùng
            var booking = db.Bookings
                .Include(b => b.Flight)
                .Include(b => b.BookingPassengers)
                .FirstOrDefault(b =>
                    b.BookingID == bookingId &&
                    b.UserID == currentUser.UserID);

            if (booking == null)
            {
                return HttpNotFound(
                    "Không tìm thấy đơn đặt vé.");
            }

            // 3. Kiểm tra trạng thái Booking
            if (booking.Status == "Confirmed")
            {
                TempData["BookingSuccess"] =
                    "Đơn đặt vé đã được thanh toán.";

                return RedirectToAction(
                    "Index", "Flights");
            }

            if (booking.Status != "Pending")
            {
                TempData["BookingError"] =
                    "Đơn đặt vé không còn khả dụng để thanh toán.";

                return RedirectToAction(
                    "Index", "Flights");
            }

            // 4. Kiểm tra thời hạn giữ ghế 10 phút
            DateTime expiresAt =
                booking.BookingDate.AddMinutes(10);

            if (DateTime.Now >= expiresAt)
            {
                TempData["BookingError"] =
                    "Đơn đặt vé đã hết thời hạn thanh toán.";

                return RedirectToAction(
                    "Index", "Flights");
            }

            // 5. Hiển thị trang thanh toán
            return View(booking);
        }

        // POST: Payment/ConfirmPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmPayment(int bookingId)
        {
            string email = User.Identity.Name;

            var currentUser = db.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Email == email);

            if (currentUser == null ||
                currentUser.Role != "Customer")
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden);
            }

            using (var transaction = db.Database.BeginTransaction(
                IsolationLevel.ReadCommitted))
            {
                try
                {
                    // 1. Khóa Booking để tránh thanh toán trùng
                    var booking = db.Bookings.SqlQuery(
                        @"SELECT *
                          FROM dbo.Bookings WITH (UPDLOCK, HOLDLOCK)
                          WHERE BookingID = @p0",
                        bookingId
                    ).SingleOrDefault();

                    if (booking == null)
                    {
                        transaction.Rollback();
                        return HttpNotFound(
                            "Không tìm thấy đơn đặt vé.");
                    }

                    // 2. Chỉ chủ đơn mới được thanh toán
                    if (booking.UserID != currentUser.UserID)
                    {
                        transaction.Rollback();
                        return new HttpStatusCodeResult(
                            HttpStatusCode.Forbidden);
                    }

                    // 3. Chống thanh toán trùng
                    if (booking.Status == "Confirmed")
                    {
                        transaction.Rollback();

                        TempData["BookingSuccess"] =
                            "Đơn đặt vé này đã được thanh toán trước đó.";

                        return RedirectToAction(
                            "Index", "Flights");
                    }

                    if (booking.Status != "Pending")
                    {
                        transaction.Rollback();

                        TempData["BookingError"] =
                            "Đơn đặt vé không còn ở trạng thái chờ thanh toán.";

                        return RedirectToAction(
                            "Index", "Flights");
                    }

                    // 4. Kiểm tra thời hạn giữ ghế
                    DateTime expiresAt =
                        booking.BookingDate.AddMinutes(10);

                    if (DateTime.Now >= expiresAt)
                    {
                        transaction.Rollback();

                        TempData["BookingError"] =
                            "Đơn đặt vé đã hết thời hạn thanh toán.";

                        return RedirectToAction(
                            "Index", "Flights");
                    }

                    // 5. Lấy các ghế thuộc Booking
                    var seatIds = db.BookingPassengers
                        .AsNoTracking()
                        .Where(p =>
                            p.BookingID == booking.BookingID)
                        .Select(p => p.FlightSeatID)
                        .ToList();

                    // Đảm bảo đủ ghế và không bị trùng
                    if (seatIds.Count != booking.PassengerCount ||
                        seatIds.Any(id => !id.HasValue) ||
                        seatIds.Distinct().Count() !=
                            booking.PassengerCount)
                    {
                        throw new InvalidOperationException(
                            "Danh sách ghế của Booking không hợp lệ.");
                    }

                    // 6. Chuyển từng ghế Held -> Booked
                    foreach (var seatId in seatIds)
                    {
                        int affected = db.Database.ExecuteSqlCommand(
                            @"UPDATE dbo.FlightSeats
                              SET Status = 'Booked'
                              WHERE FlightSeatID = @p0
                                AND FlightID = @p1
                                AND Status = 'Held'",
                            seatId.Value,
                            booking.FlightID
                        );

                        if (affected != 1)
                        {
                            throw new InvalidOperationException(
                                "Có ghế không còn ở trạng thái Held.");
                        }
                    }

                    // 7. Xác nhận Booking
                    int updatedBookings = db.Database.ExecuteSqlCommand(
                        @"UPDATE dbo.Bookings
                          SET Status = 'Confirmed'
                          WHERE BookingID = @p0
                            AND Status = 'Pending'",
                        booking.BookingID
                    );

                    if (updatedBookings != 1)
                    {
                        throw new InvalidOperationException(
                            "Không thể xác nhận Booking.");
                    }


                    // 8. Ghi nhận giao dịch thanh toán mô phỏng
                    string transactionCode =
                        "SIM-" + Guid.NewGuid().ToString("N").ToUpper();

                    var payment = new Payment
                    {
                        BookingID = booking.BookingID,
                        Amount = booking.TotalAmount,
                        PaymentMethod = "Simulation",
                        PaymentStatus = "Completed",
                        TransactionCode = transactionCode,
                        PaymentDate = DateTime.Now,
                        CreatedAt = DateTime.Now
                    };

                    db.Payments.Add(payment);
                    db.SaveChanges();

                    // 9. Hoàn tất giao dịch
                    transaction.Commit();


                    TempData["BookingSuccess"] =
                        "Thanh toán mô phỏng thành công! " +
                        "Mã đặt vé: " + booking.BookingCode;

                    return RedirectToAction(
                        "Success",
                        "Payment",
                        new { bookingId = booking.BookingID }
                    );

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("PAYMENT ERROR: " + ex.ToString());
                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception)
                    {
                        // Transaction có thể đã kết thúc
                    }

                    TempData["BookingError"] =
                        "Không thể hoàn tất thanh toán. " +
                        "Vui lòng kiểm tra lại đơn đặt vé.";

                    return RedirectToAction(
                        "Index", "Flights");
                }
            }
        }

        // GET: Payment/Success?bookingId=?
        [HttpGet]
        public ActionResult Success(int bookingId)
        {
            // 1. Xác định khách hàng đăng nhập
            string email = User.Identity.Name;

            var currentUser = db.Users
                .FirstOrDefault(u => u.Email == email);

            if (currentUser == null ||
                currentUser.Role != "Customer")
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden);
            }

            // 2. Lấy Booking thuộc về khách hàng
            var booking = db.Bookings
                .Include(b => b.Flight)
                .Include(b => b.BookingPassengers)
                .FirstOrDefault(b =>
                    b.BookingID == bookingId &&
                    b.UserID == currentUser.UserID);

            if (booking == null)
            {
                return HttpNotFound(
                    "Không tìm thấy vé điện tử.");
            }

            // 3. Chỉ hiển thị vé đã thanh toán
            if (booking.Status != "Confirmed")
            {
                TempData["BookingError"] =
                    "Đơn đặt vé chưa được thanh toán thành công.";

                return RedirectToAction(
                    "Index", "Flights");
            }

            // 4. Lấy giao dịch thanh toán thành công
            var payment = db.Payments
                .AsNoTracking()
                .FirstOrDefault(p =>
                    p.BookingID == booking.BookingID &&
                    (p.PaymentStatus == "Completed" ||
                     p.PaymentStatus == "Success"));

            if (payment == null)
            {
                TempData["BookingError"] =
                    "Chưa tìm thấy giao dịch thanh toán thành công.";

                return RedirectToAction(
                    "Index", "Flights");
            }

            // 5. Lấy thông tin sân bay từ database
            var departureAirport = db.Airports
                .AsNoTracking()
                .FirstOrDefault(a =>
                    a.AirportID == booking.Flight.DepartureAirportID);

            var arrivalAirport = db.Airports
                .AsNoTracking()
                .FirstOrDefault(a =>
                    a.AirportID == booking.Flight.ArrivalAirportID);


            // 6. Lấy số ghế của các hành khách
            var seatIds = booking.BookingPassengers
                .Where(p => p.FlightSeatID.HasValue)
                .Select(p => p.FlightSeatID.Value)
                .ToList();

            var seatNumbers = db.FlightSeats
                .AsNoTracking()
                .Where(s => seatIds.Contains(s.FlightSeatID)
                         && s.FlightID == booking.FlightID)
                .ToDictionary(
                    s => s.FlightSeatID,
                    s => s.SeatNumber
                );

            // 7. Chuyển dữ liệu sang View
            ViewBag.Payment = payment;
            ViewBag.DepartureAirport = departureAirport;
            ViewBag.ArrivalAirport = arrivalAirport;
            ViewBag.SeatNumbers = seatNumbers;

            return View(booking);

        }

        // GET: Payment/MyTickets
        [HttpGet]
        public ActionResult MyTickets()
        {
            // 1. Xác định khách hàng đang đăng nhập
            string email = User.Identity.Name;

            var currentUser = db.Users
                .FirstOrDefault(u => u.Email == email);

            if (currentUser == null ||
                currentUser.Role != "Customer")
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden);
            }

            // 2. Lấy danh sách Booking của khách hàng
            var bookings = db.Bookings
                .AsNoTracking()
                .Include(b => b.Flight)
                .Where(b => b.UserID == currentUser.UserID)
                .OrderByDescending(b => b.BookingDate)
                .ToList();

            // Lấy danh sách sân bay để hiển thị mã SGN, HAN...
            var airports = db.Airports
                .AsNoTracking()
                .ToList()
                .ToDictionary(a => a.AirportID);

            ViewBag.Airports = airports;

            // 3. Chuyển danh sách sang View
            return View(bookings);
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
