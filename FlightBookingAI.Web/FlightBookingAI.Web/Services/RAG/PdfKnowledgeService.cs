using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace FlightBookingAI.Web.Services.RAG
{
    public class PdfKnowledgeService
    {
        // Đọc toàn bộ nội dung văn bản từ file PDF.
        public string ReadPdf(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
            {
                throw new ArgumentException(
                    "Đường dẫn PDF không được để trống.");
            }

            if (!File.Exists(pdfPath))
            {
                throw new FileNotFoundException(
                    "Không tìm thấy tài liệu PDF.", pdfPath);
            }

            StringBuilder content = new StringBuilder();

            using (PdfDocument document = PdfDocument.Open(pdfPath))
            {
                foreach (var page in document.GetPages())
                {
                    content.AppendLine(
                        "=== Trang " + page.Number + " ===");

                    content.AppendLine(page.Text);
                    content.AppendLine();
                }
            }

            return content.ToString();
        }


        public List<KnowledgeChunk> ChunkPdf(
            string pdfPath,
            int chunkSize = 1000)
        {
            if (chunkSize <= 0)
                throw new ArgumentException(
                    "Kích thước chunk phải lớn hơn 0.");

            if (string.IsNullOrWhiteSpace(pdfPath))
                throw new ArgumentException(
                    "Đường dẫn PDF không được để trống.");

            if (!File.Exists(pdfPath))
                throw new FileNotFoundException(
                    "Không tìm thấy tài liệu PDF.", pdfPath);

            List<KnowledgeChunk> chunks =
                new List<KnowledgeChunk>();

            string sourceFile = Path.GetFileName(pdfPath);
            string currentDestination = "General";
            int chunkId = 1;

            // Nhận diện các tiêu đề thực tế trong tài liệu
            string headingPattern =
                @"(?<![\d.])(?:" +
                @"1\.\s*Đà Nẵng\b|" +
                @"2\.\s*Đà Lạt\b|" +
                @"3\.\s*Hạ Long\b|" +
                @"4\.\s*Phú Quốc\b|" +
                @"5\.\s*Hội An\b|" +
                @"6\.\s*Chọn điểm đến\b|" +
                @"7\.\s*Kinh nghiệm\b|" +
                @"8\.\s*Bộ câu hỏi\b|" +
                @"9\.\s*Nguồn tham khảo\b|" +
                @"10\.\s*Ngân sách dự kiến\b|" +
                @"10\.1\.\s*Cách chatbot\b|" +
                @"10\.2\.\s*Khung chi phí\b|" +
                @"10\.3\.\s*Đà Nẵng\b|" +
                @"10\.4\.\s*Đà Lạt\b|" +
                @"10\.5\.\s*Hạ Long\b|" +
                @"10\.6\.\s*Phú Quốc\b|" +
                @"10\.7\.\s*Hội An\b|" +
                @"10\.8\.\s*Ví dụ lập ngân sách\b|" +
                @"10\.9\.\s*Câu hỏi thường gặp\b|" +
                @"10\.10\.\s*Quy tắc an toàn\b" +
                @")";

            using (PdfDocument document = PdfDocument.Open(pdfPath))
            {
                foreach (var page in document.GetPages())
                {
                    string pageText = page.Text;

                    if (string.IsNullOrWhiteSpace(pageText))
                        continue;

                    // Trang 1 là giới thiệu và mục lục.
                    // Không nhận diện các mục 1-9 tại đây.
                    if (page.Number == 1)
                    {
                        AddChunks(
                            chunks,
                            pageText,
                            "General",
                            page.Number,
                            sourceFile,
                            chunkSize,
                            ref chunkId
                        );

                        continue;
                    }

                    var matches = Regex.Matches(
                        pageText,
                        headingPattern,
                        RegexOptions.IgnoreCase
                    );

                    int segmentStart = 0;

                    foreach (Match match in matches)
                    {
                        // Lưu nội dung trước tiêu đề
                        if (match.Index > segmentStart)
                        {
                            string segment = pageText.Substring(
                                segmentStart,
                                match.Index - segmentStart
                            );

                            AddChunks(
                                chunks,
                                segment,
                                currentDestination,
                                page.Number,
                                sourceFile,
                                chunkSize,
                                ref chunkId
                            );
                        }

                        // Xác định nhóm kiến thức mới
                        string heading =
                            NormalizeForHeading(match.Value);

                        if (Regex.IsMatch(heading,
                            @"^(1\.|10\.3\.)\s*da nang\b") ||
                            heading.StartsWith("10.8."))
                        {
                            currentDestination = "DaNang";
                        }
                        else if (Regex.IsMatch(heading,
                            @"^(2\.|10\.4\.)\s*da lat\b"))
                        {
                            currentDestination = "DaLat";
                        }
                        else if (Regex.IsMatch(heading,
                            @"^(3\.|10\.5\.)\s*ha long\b"))
                        {
                            currentDestination = "HaLong";
                        }
                        else if (Regex.IsMatch(heading,
                            @"^(4\.|10\.6\.)\s*phu quoc\b"))
                        {
                            currentDestination = "PhuQuoc";
                        }
                        else if (Regex.IsMatch(heading,
                            @"^(5\.|10\.7\.)\s*hoi an\b"))
                        {
                            currentDestination = "HoiAn";
                        }
                        else
                        {
                            currentDestination = "General";
                        }

                        segmentStart = match.Index;
                    }

                    // Lưu phần còn lại của trang
                    if (segmentStart < pageText.Length)
                    {
                        AddChunks(
                            chunks,
                            pageText.Substring(segmentStart),
                            currentDestination,
                            page.Number,
                            sourceFile,
                            chunkSize,
                            ref chunkId
                        );
                    }
                }
            }

            return chunks;
        }

        private string NormalizeForHeading(string text)
        {
            string normalized = text.ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

            StringBuilder builder = new StringBuilder();

            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c)
                    == UnicodeCategory.NonSpacingMark)
                    continue;

                builder.Append(c == 'đ' ? 'd' : c);
            }

            return builder.ToString();
        }

        private void AddChunks(
            List<KnowledgeChunk> chunks,
            string text,
            string destination,
            int pageNumber,
            string sourceFile,
            int chunkSize,
            ref int chunkId)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            int startIndex = 0;

            while (startIndex < text.Length)
            {
                int length = Math.Min(
                    chunkSize,
                    text.Length - startIndex
                );

                if (startIndex + length < text.Length)
                {
                    int lastSpace = text.LastIndexOf(
                        ' ',
                        startIndex + length - 1,
                        length
                    );

                    if (lastSpace > startIndex)
                        length = lastSpace - startIndex;
                }

                string content = text.Substring(
                    startIndex,
                    length
                ).Trim();

                if (!string.IsNullOrWhiteSpace(content))
                {
                    chunks.Add(new KnowledgeChunk
                    {
                        ChunkId = chunkId++,
                        Content = content,
                        PageNumber = pageNumber,
                        SourceFile = sourceFile,
                        Destination = destination
                    });
                }

                startIndex += length;

                while (startIndex < text.Length &&
                       char.IsWhiteSpace(text[startIndex]))
                {
                    startIndex++;
                }
            }
        }
    }
}
