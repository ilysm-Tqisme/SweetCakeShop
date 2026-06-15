using System.Text;
using SweetCakeShop.Models.AI;
using SweetCakeShop.Services;

namespace SweetCakeShop.Services.AI.Rag
{
    public class RagRetrieverService : IRagRetrieverService
    {
        public RagKnowledgeDocument BuildDocument(AiChatMode mode, AiBusinessContextDto context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== DỮ LIỆU CỬA HÀNG (RAG — chỉ được dùng nguồn này) ===");

            if (!context.HasData)
            {
                sb.AppendLine("Không có dữ liệu phù hợp trong hệ thống cho câu hỏi này.");
            }
            else
            {
                sb.AppendLine(context.ToSystemContextText());
            }

            sb.AppendLine("=== HẾT DỮ LIỆU ===");

            return new RagKnowledgeDocument
            {
                PrimaryFunction = context.PrimaryExecutedFunction,
                StoreDataBlock = sb.ToString(),
                Context = context
            };
        }
    }
}
