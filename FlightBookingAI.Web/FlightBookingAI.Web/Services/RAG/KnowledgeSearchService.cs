
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace FlightBookingAI.Web.Services.RAG
{
    public class KnowledgeSearchService
    {
        // Tìm các chunk phù hợp nhất với câu hỏi

        public List<KnowledgeChunk> Search(
            string question,
            List<KnowledgeChunk> chunks,
            int topK = 5)
        {
            if (string.IsNullOrWhiteSpace(question)
                || chunks == null
                || chunks.Count == 0
                || topK <= 0)
            {
                return new List<KnowledgeChunk>();
            }

            // Chuẩn hóa câu hỏi
            string normalizedQuestion = NormalizeText(question);

            // Nhận diện điểm đến
            string destination = DetectDestination(normalizedQuestion);

            // Lọc kiến thức theo điểm đến
            IEnumerable<KnowledgeChunk> filteredChunks = chunks;

            if (!string.IsNullOrEmpty(destination))
            {
                filteredChunks = chunks.Where(c =>
                    c.Destination == destination ||
                    c.Destination == "General"
                );
            }

            // Tách từ khóa
            string[] keywords = normalizedQuestion
                .Split(new[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length >= 2)
                .Distinct()
                .ToArray();

            // Xác định chủ đề câu hỏi
            string topic = DetectQuestionTopic(normalizedQuestion);

            // Chấm điểm từng chunk
            var results = filteredChunks
                .Select(chunk =>
                {
                    int keywordScore = CalculateScore(
                        normalizedQuestion,
                        keywords,
                        chunk.Content
                    );

                    // Không lấy chunk không khớp từ khóa
                    if (keywordScore <= 0)
                    {
                        return new
                        {
                            Chunk = chunk,
                            Score = 0
                        };
                    }

                    int destinationBonus =
                        chunk.Destination == destination ? 5 : 0;

                    int topicBonus = CalculateTopicBonus(
                        topic,
                        chunk.Content
                    );

                    return new
                    {
                        Chunk = chunk,
                        Score = keywordScore
                            + destinationBonus
                            + topicBonus
                    };
                })
                .Where(item => item.Score > 0)
                .OrderByDescending(item => item.Score)
                .Take(topK)
                .Select(item => item.Chunk)
                .ToList();

            return results;
        }

        private string DetectQuestionTopic(string question)
        {
            // Ưu tiên ngân sách trước lịch trình
            // vì câu hỏi chi phí cũng có thể chứa "3 ngày 2 đêm"
            if (question.Contains("ngan sach") ||
                question.Contains("chi phi") ||
                question.Contains("bao nhieu tien") ||
                question.Contains("trieu") ||
                question.Contains("gia ve") ||
                question.Contains("co du tien") ||
                question.Contains("co du khong"))
            {
                return "Budget";
            }

            // Chủ đề ăn uống
            if (question.Contains("an gi") ||
                question.Contains("mon gi") ||
                question.Contains("am thuc") ||
                question.Contains("dac san") ||
                question.Contains("quan an"))
            {
                return "Food";
            }

            // Chủ đề lưu trú
            if (question.Contains("o khu nao") ||
                question.Contains("o dau") ||
                question.Contains("luu tru") ||
                question.Contains("khach san") ||
                question.Contains("homestay") ||
                question.Contains("dat phong"))
            {
                return "Accommodation";
            }

            // Chủ đề lịch trình
            if (question.Contains("lich trinh") ||
                question.Contains("ngay") ||
                question.Contains("dem") ||
                question.Contains("di dau") ||
                question.Contains("tham quan") ||
                question.Contains("choi gi"))
            {
                return "Itinerary";
            }

            return "General";
        }

        private int CalculateTopicBonus(string topic, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return 0;

            string normalizedContent = NormalizeText(content);
            int bonus = 0;

            string[] topicKeywords;

            switch (topic)
            {
                case "Budget":
                    topicKeywords = new[]
                    {
                "ngan sach",
                "chi phi",
                "du toan",
                "trieu",
                "tiet kiem",
                "vnd",
                "phan bo",
                "phong doi"
            };
                    break;

                case "Food":
                    topicKeywords = new[]
                    {
                "an gi",
                "mon nen thu",
                "am thuc",
                "dac san",
                "mi quang",
                "banh can",
                "bun quay",
                "cao lau",
                "cha muc"
            };
                    break;

                case "Accommodation":
                    topicKeywords = new[]
                    {
                "luu tru",
                "o khu nao",
                "khach san",
                "nghi duong",
                "phong",
                "trung tam",
                "duong dong",
                "ong lang"
            };
                    break;

                case "Itinerary":
                    topicKeywords = new[]
                    {
                "lich trinh",
                "ngay 1",
                "ngay 2",
                "ngay 3",
                "tham quan",
                "tom tat lich trinh",
                "3 ngay 2 dem",
                "2 ngay 1 dem"
            };
                    break;

                default:
                    return 0;
            }

            foreach (string keyword in topicKeywords)
            {
                if (normalizedContent.Contains(keyword))
                {
                    bonus += 5;
                }
            }

            // Giới hạn điểm thưởng để không lấn át
            // hoàn toàn mức độ khớp từ khóa
            return Math.Min(bonus, 25);
        }


        // Nhận diện điểm đến trong câu hỏi
        private string DetectDestination(string question)
        {
            if (question.Contains("da nang"))
                return "DaNang";

            if (question.Contains("da lat"))
                return "DaLat";

            if (question.Contains("ha long"))
                return "HaLong";

            if (question.Contains("phu quoc"))
                return "PhuQuoc";

            if (question.Contains("hoi an"))
                return "HoiAn";

            return null;
        }

        // Tính điểm phù hợp của chunk
        private int CalculateScore(
            string question,
            string[] keywords,
            string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return 0;

            string normalizedContent = NormalizeText(content);
            int score = 0;

            string[] contentWords = normalizedContent.Split(
                new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries
            );

            foreach (string keyword in keywords)
            {
                int occurrences = contentWords.Count(
                    word => word == keyword
                );

                score += occurrences;
            }

            // Thưởng điểm khi chứa nguyên câu hỏi
            if (normalizedContent.Contains(question))
            {
                score += 10;
            }

            return score;
        }

        // Chuẩn hóa chữ thường và loại bỏ dấu tiếng Việt
        private string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string normalized = text.ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

            StringBuilder builder = new StringBuilder();

            foreach (char character in normalized)
            {
                UnicodeCategory category =
                    CharUnicodeInfo.GetUnicodeCategory(character);

                if (category != UnicodeCategory.NonSpacingMark)
                {
                    char result = character == 'đ'
                        ? 'd'
                        : character;

                    builder.Append(
                        char.IsLetterOrDigit(result)
                            ? result
                            : ' '
                    );
                }
            }

            return string.Join(
                " ",
                builder.ToString().Split(
                    new[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries
                )
            );
        }
    }
}
