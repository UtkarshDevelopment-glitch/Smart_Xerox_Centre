
namespace Smart_Xerox_Centre.DTOs.PrintSettings
{
    public class CreatePrintSettingRequest
    {
        public long OrderFileId { get; set; }

        public long? PrinterId { get; set; }

        public string ColorMode { get; set; } = "BlackWhite";

        public string Sides { get; set; } = "SingleSide";

        public string PaperSize { get; set; } = "A4";

        public string Orientation { get; set; } = "Portrait";

        public int Copies { get; set; } = 1;

        public string? PageRange { get; set; }
    }
}
