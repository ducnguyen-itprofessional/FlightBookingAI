using System;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using FlightBookingAI.Web.Helpers;
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
            string email = User.Identity.Name;

            var currentUser = db.Users
                .FirstOrDefault(u => u.Email == email);

            if (currentUser == null ||
                currentUser.Role != "Customer")
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden);
            }

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

            DateTime expiresAt =
                booking.BookingDate.AddMinutes(10);

            if (DateTime.Now >= expiresAt)
            {
                TempData["BookingError"] =
                    "Đơn đặt vé đã hết thời hạn thanh toán.";

                return RedirectToAction(
                    "Index", "Flights");
            }

            return View(booking);
        }

        // POST: Payment/ConfirmPayment (Thanh toán mô phỏng)
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

            string transactionCode = "SIM-" + Guid.NewGuid().ToString("N").ToUpper();
            return CompleteBookingPayment(bookingId, currentUser.UserID, "Simulation", transactionCode);
        }

        // POST: Payment/CreateVnPayPayment (Tạo URL chuyển hướng sang cổng VNPay)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateVnPayPayment(int bookingId)
        {
            string email = User.Identity.Name;

            var currentUser = db.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Email == email);

            if (currentUser == null || currentUser.Role != "Customer")
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            var booking = db.Bookings
                .AsNoTracking()
                .FirstOrDefault(b => b.BookingID == bookingId && b.UserID == currentUser.UserID);

            if (booking == null)
            {
                return HttpNotFound("Không tìm thấy đơn đặt vé.");
            }

            if (booking.Status != "Pending")
            {
                TempData["BookingError"] = "Đơn đặt vé không ở trạng thái chờ thanh toán.";
                return RedirectToAction("Index", "Flights");
            }

            DateTime expiresAt = booking.BookingDate.AddMinutes(10);
            if (DateTime.Now >= expiresAt)
            {
                TempData["BookingError"] = "Đơn đặt vé đã hết thời hạn thanh toán.";
                return RedirectToAction("Index", "Flights");
            }

            string vnpUrl = ConfigurationManager.AppSettings["vnp_Url"];
            string vnpTmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"];
            string vnpHashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];
            string vnpReturnUrl = ConfigurationManager.AppSettings["vnp_Returnurl"];

            if (string.IsNullOrEmpty(vnpUrl) || string.IsNullOrEmpty(vnpTmnCode) || string.IsNullOrEmpty(vnpHashSecret))
            {
                TempData["BookingError"] = "Chưa cấu hình đầy đủ thông tin cổng thanh toán VNPay trong Web.config.";
                return RedirectToAction("Index", new { bookingId = bookingId });
            }

            // Gắn BookingID vào đầu TxnRef để nhận diện chính xác đơn hàng khi VNPay gọi về
            string txnRef = booking.BookingID + "_" + DateTime.Now.Ticks;

            var vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnpTmnCode);
            // Số tiền VNPay yêu cầu nhân với 100
            vnpay.AddRequestData("vnp_Amount", ((long)(booking.TotalAmount * 100)).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", VnPayLibrary.GetIpAddress(HttpContext));
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan ve may bay ma don " + booking.BookingCode);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnpReturnUrl);
            vnpay.AddRequestData("vnp_TxnRef", txnRef);

            // Đồng bộ thời gian hết hạn thanh toán trên VNPay theo thời hạn giữ ghế
            vnpay.AddRequestData("vnp_ExpireDate", expiresAt.ToString("yyyyMMddHHmmss"));

            string paymentUrl = vnpay.CreateRequestUrl(vnpUrl, vnpHashSecret);
            return Redirect(paymentUrl);
        }

        // GET: Payment/VnPayReturn (Nhận kết quả trả về từ VNPay)
        [HttpGet]
        public ActionResult VnPayReturn()
        {
            if (Request.QueryString.Count == 0)
            {
                TempData["BookingError"] = "Không nhận được dữ liệu phản hồi từ VNPay.";
                return RedirectToAction("Index", "Flights");
            }

            string vnpHashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];
            var vnpay = new VnPayLibrary();

            foreach (string key in Request.QueryString.AllKeys)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, Request.QueryString[key]);
                }
            }

            string txnRef = vnpay.GetResponseData("vnp_TxnRef");
            string vnpTransactionNo = vnpay.GetResponseData("vnp_TransactionNo");
            string vnpResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            string vnpTransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
            string vnpSecureHash = Request.QueryString["vnp_SecureHash"];

            bool checkSignature = vnpay.ValidateSignature(vnpSecureHash, vnpHashSecret);
            if (!checkSignature)
            {
                TempData["BookingError"] = "Chữ ký bảo mật VNPay không hợp lệ!";
                return RedirectToAction("Index", "Flights");
            }

            // Tách lại BookingID từ vnp_TxnRef (định dạng: {BookingID}_{Ticks})
            int bookingId = 0;
            if (!string.IsNullOrEmpty(txnRef) && txnRef.Contains("_"))
            {
                int.TryParse(txnRef.Split('_')[0], out bookingId);
            }

            if (bookingId <= 0)
            {
                TempData["BookingError"] = "Không xác định được mã đơn đặt vé từ giao dịch VNPay.";
                return RedirectToAction("Index", "Flights");
            }

            string email = User.Identity.Name;
            var currentUser = db.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Email == email);

            if (currentUser == null || currentUser.Role != "Customer")
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            // Kiểm tra giao dịch thành công (ResponseCode == "00" và TransactionStatus == "00")
            if (vnpResponseCode == "00" && vnpTransactionStatus == "00")
            {
                string transactionCode = !string.IsNullOrEmpty(vnpTransactionNo)
                    ? "VNPAY-" + vnpTransactionNo
                    : "VNPAY-" + txnRef;

                return CompleteBookingPayment(bookingId, currentUser.UserID, "VNPay", transactionCode);
            }
            else
            {
                // Ghi nhận giao dịch thất bại / khách hủy thanh toán
                var booking = db.Bookings.AsNoTracking().FirstOrDefault(b => b.BookingID == bookingId);
                if (booking != null && booking.Status == "Pending")
                {
                    var failedPayment = new Payment
                    {
                        BookingID = booking.BookingID,
                        Amount = booking.TotalAmount,
                        PaymentMethod = "VNPay",
                        PaymentStatus = "Failed",
                        TransactionCode = !string.IsNullOrEmpty(vnpTransactionNo) ? "VNPAY-" + vnpTransactionNo : "VNPAY-FAILED-" + txnRef,
                        PaymentDate = DateTime.Now,
                        CreatedAt = DateTime.Now
                    };
                    db.Payments.Add(failedPayment);
                    db.SaveChanges();
                }

                TempData["BookingError"] = "Thanh toán VNPay không thành công hoặc đã bị hủy (Mã lỗi: " + vnpResponseCode + ").";
                return RedirectToAction("Index", new { bookingId = bookingId });
            }
        }

        // Hàm dùng chung xác nhận đặt vé & chuyển ghế Held -> Booked an toàn tuyệt đối
        private ActionResult CompleteBookingPayment(int bookingId, int userId, string paymentMethod, string transactionCode)
        {
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
                        return HttpNotFound("Không tìm thấy đơn đặt vé.");
                    }

                    // 2. Chỉ chủ đơn mới được thanh toán
                    if (booking.UserID != userId)
                    {
                        transaction.Rollback();
                        return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                    }

                    // 3. Chống thanh toán trùng
                    if (booking.Status == "Confirmed")
                    {
                        transaction.Rollback();
                        TempData["BookingSuccess"] = "Đơn đặt vé này đã được thanh toán trước đó.";
                        return RedirectToAction("Success", "Payment", new { bookingId = booking.BookingID });
                    }

                    if (booking.Status != "Pending")
                    {
                        transaction.Rollback();
                        TempData["BookingError"] = "Đơn đặt vé không còn ở trạng thái chờ thanh toán.";
                        return RedirectToAction("Index", "Flights");
                    }

                    // 4. Kiểm tra thời hạn giữ ghế
                    DateTime expiresAt = booking.BookingDate.AddMinutes(10);
                    if (DateTime.Now >= expiresAt)
                    {
                        transaction.Rollback();
                        TempData["BookingError"] = "Đơn đặt vé đã hết thời hạn thanh toán.";
                        return RedirectToAction("Index", "Flights");
                    }

                    // 5. Lấy các ghế thuộc Booking
                    var seatIds = db.BookingPassengers
                        .AsNoTracking()
                        .Where(p => p.BookingID == booking.BookingID)
                        .Select(p => p.FlightSeatID)
                        .ToList();

                    if (seatIds.Count != booking.PassengerCount ||
                        seatIds.Any(id => !id.HasValue) ||
                        seatIds.Distinct().Count() != booking.PassengerCount)
                    {
                        throw new InvalidOperationException("Danh sách ghế của Booking không hợp lệ.");
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
                            throw new InvalidOperationException("Có ghế không còn ở trạng thái Held.");
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
                        throw new InvalidOperationException("Không thể xác nhận Booking.");
                    }

                    // 8. Ghi nhận giao dịch thanh toán (Simulation hoặc VNPay)
                    var payment = new Payment
                    {
                        BookingID = booking.BookingID,
                        Amount = booking.TotalAmount,
                        PaymentMethod = paymentMethod,
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
                        "Thanh toán qua " + paymentMethod + " thành công! Mã đặt vé: " + booking.BookingCode;

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
                    }

                    TempData["BookingError"] =
                        "Không thể hoàn tất thanh toán. Vui lòng kiểm tra lại đơn đặt vé.";

                    return RedirectToAction("Index", "Flights");
                }
            }
        }

        // GET: Payment/Success?bookingId=?
        [HttpGet]
        public ActionResult Success(int bookingId)
        {
            string email = User.Identity.Name;

            var currentUser = db.Users
                .FirstOrDefault(u => u.Email == email);

            if (currentUser == null ||
                currentUser.Role != "Customer")
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden);
            }

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

            if (booking.Status != "Confirmed")
            {
                TempData["BookingError"] =
                    "Đơn đặt vé chưa được thanh toán thành công.";

                return RedirectToAction(
                    "Index", "Flights");
            }

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

            var departureAirport = db.Airports
                .AsNoTracking()
                .FirstOrDefault(a =>
                    a.AirportID == booking.Flight.DepartureAirportID);

            var arrivalAirport = db.Airports
                .AsNoTracking()
                .FirstOrDefault(a =>
                    a.AirportID == booking.Flight.ArrivalAirportID);

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
            string email = User.Identity.Name;

            var currentUser = db.Users
                .FirstOrDefault(u => u.Email == email);

            if (currentUser == null ||
                currentUser.Role != "Customer")
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Forbidden);
            }

            var bookings = db.Bookings
                .AsNoTracking()
                .Include(b => b.Flight)
                .Where(b => b.UserID == currentUser.UserID)
                .OrderByDescending(b => b.BookingDate)
                .ToList();

            var airports = db.Airports
                .AsNoTracking()
                .ToList()
                .ToDictionary(a => a.AirportID);

            ViewBag.Airports = airports;

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