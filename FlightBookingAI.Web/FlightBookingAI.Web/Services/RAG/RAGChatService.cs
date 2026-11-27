
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightBookingAI.Web.Services.RAG
{
    public class RAGChatService
    {
        private readonly PdfKnowledgeService pdfService;
        private readonly KnowledgeSearchService searchService;
        private readonly GeminiAIService geminiService;

        public RAGChatService()
        {
            pdfService = new PdfKnowledgeService();
            searchService = new KnowledgeSearchService();
            geminiService = new GeminiAIService();
        }

        // Xử lý câu hỏi bằng RAG
        public async Task<string> AskAsync(
            string question,
            string pdfPath)
        {
            // 1. Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(question))
            {
                return "Vui lòng nhập câu hỏi về du lịch.";
            }

            if (string.IsNullOrWhiteSpace(pdfPath) ||
                !System.IO.File.Exists(pdfPath))
            {
                return "Không tìm thấy tài liệu kiến thức du lịch.";
            }

            // 2. Đọc PDF và chia thành các chunk
            List<KnowledgeChunk> chunks =
                pdfService.ChunkPdf(pdfPath, 1000);

            if (chunks == null || chunks.Count == 0)
            {
                return "Tài liệu PDF chưa có nội dung để tư vấn.";
            }

            // 3. Tìm 5 chunk phù hợp nhất
            List<KnowledgeChunk> relevantChunks =
                searchService.Search(question, chunks, 5);

            if (relevantChunks.Count == 0)
            {
                return "Xin lỗi, tôi chưa tìm thấy thông tin " +
                       "phù hợp trong tài liệu du lịch.";
            }

            // 4. Tạo ngữ cảnh từ các chunk
            StringBuilder contextBuilder = new StringBuilder();

            foreach (KnowledgeChunk chunk in relevantChunks)
            {
                contextBuilder.AppendLine(
                    "[Nguồn: " + chunk.SourceFile +
                    " | Trang: " + chunk.PageNumber +
                    " | Điểm đến: " + chunk.Destination + "]"
                );

                contextBuilder.AppendLine(chunk.Content);
                contextBuilder.AppendLine();
            }

            // 5. Tạo prompt giới hạn kiến thức
            string prompt = @"
Bạn là AI Travel Assistant của FlightBookingAI.

NHIỆM VỤ:
- Tư vấn du lịch bằng tiếng Việt.
- Chỉ sử dụng thông tin có trong phần TÀI LIỆU THAM KHẢO.
- Không tự bổ sung giá vé, khách sạn, địa điểm,
  lịch trình hoặc chi phí ngoài tài liệu.
- Không khẳng định thông tin chưa được tài liệu hỗ trợ.
- Nếu tài liệu không đủ thông tin để trả lời,
  hãy nói rõ rằng tài liệu chưa cung cấp thông tin đó.
- Nếu câu hỏi nằm ngoài phạm vi tư vấn du lịch
  của tài liệu, hãy lịch sự từ chối.
- Trả lời rõ ràng, dễ hiểu và thân thiện.
QUY TẮC HIỂN THỊ NGUỒN:
- Chỉ sử dụng tài liệu tham khảo để xác định nội dung trả lời.
- Không hiển thị tên file PDF trong câu trả lời.
- Không hiển thị số trang hoặc ký hiệu [Nguồn: ...].
- Không chèn trích dẫn sau từng câu hoặc từng mục danh sách.
- Không tạo mục ""Nguồn tham khảo"" ở cuối câu trả lời.
- Vẫn phải tuân thủ nghiêm ngặt phạm vi kiến thức của tài liệu.

QUY TẮC TRÌNH BÀY CÂU TRẢ LỜI:
- Sử dụng Markdown để định dạng.
- Mỗi đoạn văn chỉ nên dài 2–3 câu.
- Chỉ xuống dòng khi kết thúc một ý hoàn chỉnh.
- Không ngắt dòng giữa tên một khoản chi phí và số tiền.
- Với danh sách, mỗi mục phải nằm trên một dòng Markdown riêng.
- In đậm các thông tin quan trọng như điểm đến, số ngày,
  ngân sách, tổng chi phí và các lưu ý quan trọng.
- Không in đậm toàn bộ đoạn văn.
- Khi liệt kê chi phí, ưu tiên định dạng:
  - **Tên khoản chi:** Số tiền được trích từ tài liệu
- Không tự tạo số liệu hoặc thông tin ngoài tài liệu PDF.
- Giữ câu trả lời gọn gàng, dễ đọc trên cả máy tính và điện thoại.


TÀI LIỆU THAM KHẢO:
" + contextBuilder.ToString() + @"

CÂU HỎI NGƯỜI DÙNG:
" + question + @"

Hãy trả lời câu hỏi dựa trên tài liệu tham khảo.";

            // 6. Gửi prompt đến Gemini
            string answer =
                await geminiService.GenerateResponseAsync(prompt);

            return answer;
        }

        // Tạo lịch trình du lịch dạng JSON bằng RAG
        public async Task<string> GenerateItineraryAsync(
            string destination,
            int days,
            string budget,
            string interests,
            string pdfPath)
        {
            // 1. Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(destination))
            {
                throw new ArgumentException(
                    "Vui lòng nhập điểm đến.");
            }

            if (days < 1 || days > 30)
            {
                throw new ArgumentException(
                    "Số ngày du lịch phải từ 1 đến 30.");
            }

            if (string.IsNullOrWhiteSpace(pdfPath) ||
                !System.IO.File.Exists(pdfPath))
            {
                throw new System.IO.FileNotFoundException(
                    "Không tìm thấy tài liệu kiến thức du lịch.");
            }

            // 2. Đọc PDF và chia thành các chunk
            List<KnowledgeChunk> chunks =
                pdfService.ChunkPdf(pdfPath, 1000);

            if (chunks == null || chunks.Count == 0)
            {
                throw new InvalidOperationException(
                    "Tài liệu PDF chưa có nội dung.");
            }

            // 3. Tạo câu hỏi để truy xuất kiến thức
            string searchQuestion =
                "Lịch trình du lịch " + destination +
                " " + days + " ngày, địa điểm tham quan, " +
                "hoạt động trải nghiệm, ẩm thực, " +
                "sở thích " + interests +
                ", ngân sách " + budget;

            // 4. Tìm các chunk liên quan
            List<KnowledgeChunk> relevantChunks =
                searchService.Search(
                    searchQuestion,
                    chunks,
                    8);

            if (relevantChunks == null ||
                relevantChunks.Count == 0)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy kiến thức phù hợp " +
                    "để tạo lịch trình.");
            }

            // 5. Tạo ngữ cảnh từ các chunk
            StringBuilder contextBuilder = new StringBuilder();

            foreach (KnowledgeChunk chunk in relevantChunks)
            {
                contextBuilder.AppendLine(
                    "[Điểm đến: " + chunk.Destination +
                    " | Trang: " + chunk.PageNumber + "]");

                contextBuilder.AppendLine(chunk.Content);
                contextBuilder.AppendLine();
            }

            // 6. Gọi Gemini tạo JSON
            string itineraryJson =
                await geminiService.GenerateItineraryJsonAsync(
                    destination,
                    days,
                    budget ?? "",
                    interests ?? "",
                    contextBuilder.ToString());

            return itineraryJson;
        }

    }
}
