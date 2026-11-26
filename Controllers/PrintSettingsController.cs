
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.DTOs.PrintSettings;
using Smart_Xerox_Centre.Models;
using System.Security.Cryptography;
using System.Text;

namespace Smart_Xerox_Centre.Controllers
{
    [ApiController]
    [Route("api/print-settings")]
    [AllowAnonymous]
    public class PrintSettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public PrintSettingsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // POST: api/print-settings/create
        [HttpPost("create")]
        public async Task<IActionResult> CreatePrintSetting(
            [FromBody] CreatePrintSettingRequest request,
            [FromHeader(Name = "X-Session-Token")] string sessionToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                return Unauthorized("Session token is required.");

            if (request.OrderFileId <= 0)
                return BadRequest("Valid OrderFileId is required.");

            if (request.Copies < 1 || request.Copies > 100)
                return BadRequest("Copies must be between 1 and 100.");

            if (request.ColorMode != "BlackWhite" &&
                request.ColorMode != "Color")
                return BadRequest("ColorMode must be BlackWhite or Color.");

            if (request.Sides != "SingleSide" &&
                request.Sides != "DoubleSide")
                return BadRequest("Sides must be SingleSide or DoubleSide.");

            if (request.PaperSize != "A4")
                return BadRequest("Currently only A4 paper size is configured.");

            if (request.Orientation != "Portrait" &&
                request.Orientation != "Landscape")
                return BadRequest("Orientation must be Portrait or Landscape.");

            // 1. Validate customer session
            string tokenHash = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(sessionToken)));

            DateTime now = DateTime.UtcNow;

            var session = await _db.CustomerSessions
                .FirstOrDefaultAsync(x =>
                    x.SessionTokenHash == tokenHash &&
                    x.EndedAtUtc == null &&
                    x.ExpiresAtUtc > now);

            if (session == null)
                return Unauthorized("Session is invalid or expired.");

            // 2. Find uploaded document and its Order
            var orderFile = await _db.OrderFiles
                .Include(x => x.Order)
                .FirstOrDefaultAsync(x =>
                    x.OrderFileId == request.OrderFileId &&
                    x.Order.TenantId == session.TenantId &&
                    x.Order.ShopId == session.ShopId &&
                    x.Order.SessionId == session.SessionId);

            if (orderFile == null)
                return NotFound("Document not found for this session.");

            var order = orderFile.Order;

            if (order.OrderStatus != "Created")
                return BadRequest("Print settings can only be added to Created orders.");

            // 3. Page count must be available
            if (orderFile.PageCount == null || orderFile.PageCount <= 0)
                return BadRequest("PDF page count is not available. Calculate page count first.");

            // 4. Prevent duplicate settings for same file
            bool alreadyExists = await _db.PrintSettings
                .AnyAsync(x => x.OrderFileId == request.OrderFileId);

            if (alreadyExists)
                return Conflict("Print settings already exist for this document.");

            // 5. Calculate selected pages
            int pagesToPrint;

            if (!TryCalculatePages(
                request.PageRange,
                orderFile.PageCount.Value,
                out pagesToPrint,
                out string pageError))
            {
                return BadRequest(pageError);
            }

            // 6. Validate optional Printer
            if (request.PrinterId.HasValue)
            {
                bool printerExists = await _db.Printers
                    .AnyAsync(x =>
                        x.PrinterId == request.PrinterId.Value &&
                        x.TenantId == session.TenantId &&
                        x.ShopId == session.ShopId);

                if (!printerExists)
                    return BadRequest("Selected printer does not belong to this shop.");
            }

            // 7. Find active Shop Pricing
            var pricing = await _db.ShopPricings
                .Where(x =>
                    x.TenantId == session.TenantId &&
                    x.ShopId == session.ShopId &&
                    x.ColorMode == request.ColorMode &&
                    x.Sides == request.Sides &&
                    x.PaperSize == request.PaperSize &&
                    x.IsActive &&
                    x.EffectiveFromUtc <= now &&
                    (x.EffectiveToUtc == null ||
                     x.EffectiveToUtc > now))
                .OrderByDescending(x => x.EffectiveFromUtc)
                .FirstOrDefaultAsync();

            if (pricing == null)
                return BadRequest("Active pricing not found for selected print settings.");

            // 8. Calculate amount
            decimal baseAmount =
                pagesToPrint *
                request.Copies *
                pricing.RatePerPage;

            decimal additionalCharge = pricing.AdditionalCharge;

            decimal taxAmount =
                (baseAmount + additionalCharge) *
                pricing.TaxRate / 100m;

            decimal lineTotal =
                baseAmount + additionalCharge + taxAmount;

            // 9. Create PrintSetting record
            var printSetting = new PrintSetting
            {
                OrderFileId = orderFile.OrderFileId,
                PrinterId = request.PrinterId,
                ColorMode = request.ColorMode,
                Sides = request.Sides,
                PaperSize = request.PaperSize,
                Orientation = request.Orientation,
                Copies = request.Copies,
                PageRange = string.IsNullOrWhiteSpace(request.PageRange)
                    ? null
                    : request.PageRange.Trim(),
                PagesToPrint = pagesToPrint,
                RatePerPage = pricing.RatePerPage,
                LineTotal = lineTotal,
                CreatedAtUtc = now
            };

            _db.PrintSettings.Add(printSetting);

            // 10. Update Order amounts
            // This calculation assumes one PrintSetting per OrderFile.
            order.SubTotal = baseAmount;
            order.AdditionalCharges = additionalCharge;
            order.TaxAmount = taxAmount;
            order.DiscountAmount = 0;
            order.TotalAmount = lineTotal;
            order.UpdatedAtUtc = now;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Print settings saved and amount calculated successfully.",

                printSetting = new
                {
                    printSetting.PrintSettingId,
                    printSetting.OrderFileId,
                    printSetting.PrinterId,
                    printSetting.ColorMode,
                    printSetting.Sides,
                    printSetting.PaperSize,
                    printSetting.Orientation,
                    printSetting.Copies,
                    printSetting.PageRange,
                    printSetting.PagesToPrint,
                    printSetting.RatePerPage,
                    printSetting.LineTotal
                },

                calculation = new
                {
                    pdfPageCount = orderFile.PageCount,
                    pagesSelected = pagesToPrint,
                    copies = request.Copies,
                    ratePerPage = pricing.RatePerPage,
                    baseAmount,
                    additionalCharge,
                    taxRate = pricing.TaxRate,
                    taxAmount,
                    totalAmount = lineTotal
                },

                order = new
                {
                    order.OrderId,
                    order.OrderNumber,
                    order.OrderStatus,
                    order.SubTotal,
                    order.AdditionalCharges,
                    order.TaxAmount,
                    order.DiscountAmount,
                    order.TotalAmount
                }
            });
        }

        // Page Range examples:
        // null = all pages
        // 1-3
        // 1,3,5
        // 1-3,5
        private static bool TryCalculatePages(
            string? pageRange,
            int totalPages,
            out int pagesToPrint,
            out string error)
        {
            pagesToPrint = 0;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(pageRange))
            {
                pagesToPrint = totalPages;
                return true;
            }

            var selectedPages = new HashSet<int>();

            string[] parts = pageRange.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                error = "Invalid page range.";
                return false;
            }

            foreach (string part in parts)
            {
                if (part.Contains('-'))
                {
                    string[] range = part.Split('-');

                    if (range.Length != 2 ||
                        !int.TryParse(range[0], out int start) ||
                        !int.TryParse(range[1], out int end) ||
                        start < 1 ||
                        end < start ||
                        end > totalPages)
                    {
                        error = $"Invalid page range: {part}";
                        return false;
                    }

                    for (int page = start; page <= end; page++)
                    {
                        if (!selectedPages.Add(page))
                        {
                            error = "Duplicate pages are not allowed in PageRange.";
                            return false;
                        }
                    }
                }
                else
                {
                    if (!int.TryParse(part, out int page) ||
                        page < 1 ||
                        page > totalPages)
                    {
                        error = $"Invalid page number: {part}";
                        return false;
                    }

                    if (!selectedPages.Add(page))
                    {
                        error = "Duplicate pages are not allowed in PageRange.";
                        return false;
                    }
                }
            }

            pagesToPrint = selectedPages.Count;

            if (pagesToPrint == 0)
            {
                error = "At least one page must be selected.";
                return false;
            }

            return true;
        }
    }
}
