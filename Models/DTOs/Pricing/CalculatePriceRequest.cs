namespace Smart_Xerox_Centre.DTOs.Pricing;

public sealed class CalculatePriceRequest
{
    public long ShopId { get; set; }
    public string ColorMode { get; set; } = "BlackWhite";
    public string Sides { get; set; } = "Single";
    public string PaperSize { get; set; } = "A4";
    public int PagesToPrint { get; set; }
    public int Copies { get; set; } = 1;
}
