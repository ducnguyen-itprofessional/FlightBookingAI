
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlightBookingAI.Web.Services.RAG
{
    public class GeminiAIService
    {
        private static readonly HttpClient httpClient =
            new HttpClient();

        // Model sử dụng cho chatbot
        private const string ModelName = "gemini-3.5-flash-lite";

        // Gửi câu hỏi đến Gemini API
        public async Task<string> GenerateResponseAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException(
                    "Nội dung câu hỏi không được để trống.");
            }

            // Lấy API Key từ biến môi trường Windows
            string apiKey = Environment.GetEnvironmentVariable(
                "GEMINI_API_KEY"
            );

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Không tìm thấy biến môi trường GEMINI_API_KEY.");
            }

            string url =
                "https://generativelanguage.googleapis.com/v1beta/models/"
                + ModelName + ":generateContent";

            // Tạo dữ liệu gửi đến Gemini
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    maxOutputTokens = 1024
                }
            };

            string json = JsonConvert.SerializeObject(requestBody);


            using (HttpResponseMessage response =
                await SendWithRetryAsync(url, apiKey, json))
            {
                string responseJson =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = response.ReasonPhrase;

                    try
                    {
                        JObject errorResult = JObject.Parse(responseJson);

                        errorMessage =
                            errorResult["error"]?["message"]?.ToString()
                            ?? response.ReasonPhrase;
                    }
                    catch (JsonException)
                    {
                        // Giữ thông báo HTTP nếu JSON không hợp lệ
                    }

                    throw new HttpRequestException(
                        "Gemini API lỗi HTTP "
                        + (int)response.StatusCode
                        + ": " + errorMessage);
                }

                JObject result = JObject.Parse(responseJson);

                JToken parts =
                    result["candidates"]?[0]?["content"]?["parts"];

                if (parts == null)
                {
                    throw new InvalidOperationException(
                        "Gemini không trả về nội dung.");
                }

                StringBuilder answer = new StringBuilder();

                foreach (JToken part in parts)
                {
                    string text = part["text"]?.ToString();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        answer.AppendLine(text);
                    }
                }

                if (answer.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Gemini trả về phản hồi rỗng.");
                }

                return answer.ToString().Trim();
            }

        }

        public async Task<string> GenerateItineraryJsonAsync(
            string destination,
            int days,
            string budget,
            string interests,
            string knowledgeContext)
        {
            string prompt = @"
Bạn là AI Travel Assistant của FlightBookingAI.

NHIỆM VỤ:
Tạo lịch trình du lịch dựa trên tài liệu tham khảo được cung cấp.

THÔNG TIN CHUYẾN ĐI:
- Điểm đến: " + destination + @"
- Số ngày: " + days + @"
- Ngân sách: " + budget + @"
- Sở thích: " + interests + @"

QUY TẮC:
1. Chỉ sử dụng địa điểm tham quan và món ăn có trong tài liệu.
2. Không tự sáng tạo địa điểm hoặc món ăn ngoài tài liệu.
3. Mỗi ngày có một tiêu đề ngắn gọn.
4. Mỗi ngày ưu tiên 2 đến 4 địa điểm tham quan nếu tài liệu có đủ.
5. Gợi ý món ăn ngắn gọn, nếu tài liệu không có thì để chuỗi rỗng.
6. Không đưa ngân sách hoặc lời chào vào các thẻ lịch trình.
7. Số ngày trong JSON phải bằng số ngày được yêu cầu nếu tài liệu
   có đủ thông tin để xây dựng lịch trình phù hợp.
8. Nếu tài liệu không đủ thông tin, không tự bịa thêm.
9. Chỉ trả về JSON hợp lệ, không dùng Markdown,
   không thêm ```json hoặc lời giải thích.

ĐỊNH DẠNG JSON:
{
  ""destination"": ""Tên điểm đến"",
  ""days"": [
    {
      ""day"": 1,
      ""title"": ""Tiêu đề ngày 1"",
      ""places"": [""Địa điểm 1"", ""Địa điểm 2""],
      ""food"": ""Món ăn gợi ý""
    }
  ]
}

TÀI LIỆU THAM KHẢO:
" + knowledgeContext;

            return await GenerateResponseAsync(prompt);
        }
        private async Task<HttpResponseMessage> SendWithRetryAsync(
            string url,
            string apiKey,
            string json)
        {
            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using (var request = new HttpRequestMessage(
                    HttpMethod.Post, url))
                {
                    request.Headers.Add("x-goog-api-key", apiKey);

                    request.Content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json"
                    );

                    HttpResponseMessage response =
                        await httpClient.SendAsync(request);

                    // Chỉ thử lại khi server quá tải hoặc
                    // giới hạn tạm thời.
                    bool shouldRetry =
                        response.StatusCode ==
                            System.Net.HttpStatusCode.ServiceUnavailable ||
                        response.StatusCode ==
                            System.Net.HttpStatusCode.BadGateway ||
                        response.StatusCode ==
                            System.Net.HttpStatusCode.GatewayTimeout;

                    if (!shouldRetry || attempt == maxAttempts)
                    {
                        return response;
                    }

                    response.Dispose();

                    // Chờ 2 giây, rồi 4 giây
                    await Task.Delay(2000 * attempt);
                }
            }

            throw new InvalidOperationException(
                "Không thể gửi yêu cầu đến Gemini API.");
        }

    }
}
