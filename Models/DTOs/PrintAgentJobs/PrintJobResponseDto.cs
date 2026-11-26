
namespace Smart_Xerox_Centre.DTOs.PrintAgentJobs
{
    public class PrintJobResponseDto
    {
        public long PrintJobId { get; set; }

        public Guid JobGuid { get; set; }

        public long OrderId { get; set; }

        public long TenantId { get; set; }

        public long ShopId { get; set; }

        public long? PrinterId { get; set; }

        public int Priority { get; set; }

        public string JobStatus { get; set; } = string.Empty;

        public int AttemptNumber { get; set; }

        public DateTime QueuedAtUtc { get; set; }
    }
}
