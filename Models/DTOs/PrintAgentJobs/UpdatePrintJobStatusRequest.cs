
namespace Smart_Xerox_Centre.DTOs.PrintAgentJobs
{
    public class UpdatePrintJobStatusRequest
    {
        public long PrintJobId { get; set; }

        public string JobStatus { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public string? WindowsSpoolerJobId { get; set; }
    }
}
