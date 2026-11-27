using System.Linq;
using System.Text;
using System.Globalization;
using Newtonsoft.Json;
using FlightBookingAI.Web.Models;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using FlightBookingAI.Web.Services.RAG;

namespace FlightBookingAI.Web.Controllers
{
    public class AItravelController : Controller
    {
        // Trang AI Travel Assistant
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Ask(string question)
        {
            // 1. Kiểm tra câu hỏi
            if (string.IsNullOrWhiteSpace(question))
            {
                return Json(new
                {
                    success = false,
                    message = "Vui lòng nhập câu hỏi."
                });
            }

            if (question.Length > 1000)
            {
                return Json(new
                {
                    success = false,
                    message = "Câu hỏi không được vượt quá 1000 ký tự."
                });
            }

            try
            {
                // 2. Xác định đường dẫn PDF
                string pdfPath = Server.MapPath(
                    "~/App_Data/KnowledgeBase/FlightBookingAI_Travel_Knowledge.pdf"
                );

                // 3. Gọi RAG
                var ragService = new RAGChatService();

                string answer = await ragService.AskAsync(
                    question,
                    pdfPath
                );

                // 4. Trả câu trả lời về JavaScript
                return Json(new
                {
                    success = true,
                    answer = answer
                });
            }
            catch (Exception)
            {
                // Không hiển thị chi tiết lỗi API cho người dùng
                return Json(new
                {
                    success = false,
                    message = "Trợ lý AI hiện chưa thể trả lời. " +
                              "Vui lòng thử lại sau."
                });
            }
        }

        // Tạo lịch trình du lịch AI theo từng ngày
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GenerateItinerary(
            string destination,
            int days,
            string budget,
            string interests)
        {
            // 1. Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(destination))
            {
                return Json(new
                {
                    success = false,
                    message = "Vui lòng nhập điểm đến."
                });
            }

            if (days < 1 || days > 30)
            {
                return Json(new
                {
                    success = false,
                    message = "Số ngày phải từ 1 đến 30."
                });
            }

            // Chuẩn hóa tên điểm đến
            string normalizedDestination =
                RemoveVietnameseSigns(destination);

            string[] supportedDestinations =
            {
                "da nang",
                "da lat",
                "ha long",
                "phu quoc",
                "hoi an"
            };

            if (!supportedDestinations.Contains(normalizedDestination))
            {
                return Json(new
                {
                    success = false,
                    message = "Hiện tại AI hỗ trợ 5 điểm đến: " +
                              "Đà Nẵng, Đà Lạt, Hạ Long, " +
                              "Phú Quốc và Hội An."
                });
            }

            // 2. Gọi RAG + Gemini
            try
            {
                string pdfPath = Server.MapPath(
                    "~/App_Data/KnowledgeBase/FlightBookingAI_Travel_Knowledge.pdf"
                );

                var ragService = new RAGChatService();

                string rawJson =
                    await ragService.GenerateItineraryAsync(
                        destination,
                        days,
                        budget ?? "",
                        interests ?? "",
                        pdfPath
                    );

                // 3. Chuyển phản hồi JSON thành Model
                string cleanedJson = (rawJson ?? "").Trim();

                // Dự phòng nếu Gemini trả Markdown code fence
                if (cleanedJson.StartsWith("```"))
                {
                    int firstNewLine = cleanedJson.IndexOf('\n');
                    int lastFence = cleanedJson.LastIndexOf("```");

                    if (firstNewLine >= 0 &&
                        lastFence > firstNewLine)
                    {
                        cleanedJson = cleanedJson.Substring(
                            firstNewLine + 1,
                            lastFence - firstNewLine - 1
                        ).Trim();
                    }
                }

                AIItineraryViewModel itinerary =
                    JsonConvert.DeserializeObject<AIItineraryViewModel>(
                        cleanedJson
                    );

                // 4. Kiểm tra cấu trúc dữ liệu
                if (itinerary == null ||
                    itinerary.Days == null ||
                    itinerary.Days.Count == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "AI chưa tạo được lịch trình phù hợp. " +
                                  "Vui lòng thử lại."
                    });
                }

                // Chỉ nhận các ngày hợp lệ
                itinerary.Days = itinerary.Days
                    .Where(d => d != null &&
                                d.Day >= 1 &&
                                d.Day <= days &&
                                d.Places != null &&
                                d.Places.Any(p =>
                                    !string.IsNullOrWhiteSpace(p)))
                    .GroupBy(d => d.Day)
                    .Select(g => g.First())
                    .OrderBy(d => d.Day)
                    .ToList();

                if (itinerary.Days.Count != days ||
                    !itinerary.Days.Select(d => d.Day)
                        .SequenceEqual(Enumerable.Range(1, days)))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Tài liệu chưa cung cấp đủ thông tin " +
                                  "để tạo lịch trình theo số ngày yêu cầu."
                    });
                }

                // 5. Trả dữ liệu về giao diện
                return Json(new
                {
                    success = true,
                    itinerary = itinerary
                });
            }
            catch (JsonException)
            {
                return Json(new
                {
                    success = false,
                    message = "AI trả về dữ liệu lịch trình chưa đúng " +
                              "định dạng. Vui lòng thử lại."
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "Không thể tạo lịch trình lúc này. " +
                              "Vui lòng thử lại sau."
                });
            }
        }

        // Chuẩn hóa tên điểm đến để so sánh
        private string RemoveVietnameseSigns(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            string normalized = text.Trim()
                .ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

            StringBuilder builder = new StringBuilder();

            foreach (char character in normalized)
            {
                UnicodeCategory category =
                    CharUnicodeInfo.GetUnicodeCategory(character);

                if (category == UnicodeCategory.NonSpacingMark)
                    continue;

                char result = character == 'đ' ? 'd' : character;

                builder.Append(
                    char.IsLetterOrDigit(result) ||
                    char.IsWhiteSpace(result)
                        ? result
                        : ' '
                );
            }

            return string.Join(" ",
                builder.ToString().Split(
                    new[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries));
        }

    }
}
