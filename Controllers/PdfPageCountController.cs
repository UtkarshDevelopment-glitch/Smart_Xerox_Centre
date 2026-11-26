
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using UglyToad.PdfPig;

namespace Smart_Xerox_Centre.Controllers
{
    [ApiController]
    [Route("api/documents")]
    public class PdfPageCountController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _environment;

        public PdfPageCountController(
            ApplicationDbContext db,
            IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        // POST: api/documents/1/page-count
        [HttpPost("{orderFileId:long}/page-count")]
        public async Task<IActionResult> GetPdfPageCount(
            long orderFileId,
            [FromHeader(Name = "X-Session-Token")] string sessionToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                return Unauthorized("Session token is required.");

            // Validate session token
            var tokenHash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(sessionToken)));

            var now = DateTime.UtcNow;

            var session = await _db.CustomerSessions
                .FirstOrDefaultAsync(x =>
                    x.SessionTokenHash == tokenHash &&
                    x.EndedAtUtc == null &&
                    x.ExpiresAtUtc > now);

            if (session == null)
                return Unauthorized("Session is invalid or expired.");

            // Find uploaded file belonging to this session
            var orderFile = await _db.OrderFiles
                .Include(x => x.Order)
                .FirstOrDefaultAsync(x =>
                    x.OrderFileId == orderFileId &&
                    x.Order.TenantId == session.TenantId &&
                    x.Order.ShopId == session.ShopId &&
                    x.Order.SessionId == session.SessionId);

            if (orderFile == null)
                return NotFound("Document not found for this session.");

            if (!string.Equals(
                orderFile.FileExtension,
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Page count is currently supported for PDF files only.");
            }

            // Physical file path
            string relativePath = orderFile.StoragePath.Replace(
                '/',
                Path.DirectorySeparatorChar);

            string filePath = Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                relativePath);

            if (!System.IO.File.Exists(filePath))
                return NotFound("Uploaded PDF file not found on server.");

            try
            {
                int pageCount;

                using (var pdfDocument = PdfDocument.Open(filePath))
                {
                    pageCount = pdfDocument.NumberOfPages;
                }

                if (pageCount <= 0)
                    return BadRequest("PDF does not contain any pages.");

                // Update OrderFiles table
                orderFile.PageCount = pageCount;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "PDF page count calculated successfully.",
                    orderFileId = orderFile.OrderFileId,
                    orderId = orderFile.OrderId,
                    originalFileName = orderFile.OriginalFileName,
                    pageCount = orderFile.PageCount,
                    status = "Success"
                });
            }
            catch (Exception)
            {
                return BadRequest(
                    "Unable to read PDF. The file may be damaged or invalid.");
            }
        }
    }
}
