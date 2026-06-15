namespace SweetCakeShop.Services.AI
{
    public interface IOrderHandoffService
    {
        void UpdateFromMessage(ConversationSessionState state, string message);
        string BuildRagSupplement(ConversationSessionState state);
        bool RequestsHumanAgent(string message);
        string GetImmediateHandoffReply(string languageCode = "vi");
    }
}
