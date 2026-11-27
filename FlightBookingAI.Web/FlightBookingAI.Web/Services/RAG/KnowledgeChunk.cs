
namespace FlightBookingAI.Web.Services.RAG
{
    public class KnowledgeChunk
    {
        // Mã định danh của đoạn kiến thức
        public int ChunkId { get; set; }

        // Nội dung văn bản của đoạn kiến thức
        public string Content { get; set; }

        // Số trang PDF chứa đoạn kiến thức
        public int PageNumber { get; set; }

        // Tên tài liệu PDF nguồn
        public string SourceFile { get; set; }

        // Điểm đến mà đoạn kiến thức thuộc về
        public string Destination { get; set; }
    }
}
