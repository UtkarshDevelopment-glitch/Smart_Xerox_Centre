
namespace Smart_Xerox_Centre.DTOs.PrintAgents
{
    public class RegisterPrintAgentRequest
    {
        public long ShopId { get; set; }

        public string AgentName { get; set; } = string.Empty;

        public string? MachineName { get; set; }

        public string? AgentVersion { get; set; }
    }
}
