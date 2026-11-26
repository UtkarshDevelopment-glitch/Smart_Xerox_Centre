
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Models;
using System.Security.Cryptography;
using System.Text;

namespace Smart_Xerox_Centre.Controllers;

[ApiController]
[Route("api/documents")]
[AllowAnonymous]
public class DocumentUploadController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _environment;

    private const long MaxFileSize = 25 * 1024 * 1024;

    private static readonly string[] AllowedExtensions =
    {
        ".pdf", ".doc", ".docx",
        ".jpg", ".jpeg", ".png"
    };

    public DocumentUploadController(
        ApplicationDbContext db,
        IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromHeader(Name = "X-Session-Token")] string sessionToken,
        [FromHeader(Name = "X-Order-Id")] long orderId)
    {
        string? fullPath = null;

        try
        {
            // 1. Validate Session Token
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                return Unauthorized(new
                {
                    message = "Session token is required."
                });
            }

            // 2. Validate OrderId
            if (orderId <= 0)
            {
                return BadRequest(new
                {
                    message = "Valid OrderId is required."
                });
            }

            // 3. Generate SHA256 Hash
            string tokenHash = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(sessionToken)));

            DateTime now = DateTime.UtcNow;

            // 4. Validate Customer Session
            var session = await _db.CustomerSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.SessionTokenHash == tokenHash &&
                    x.EndedAtUtc == null &&
                    x.ExpiresAtUtc > now);

            if (session == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid or expired session."
                });
            }

            // 5. Validate Order belongs to this Session
            var order = await _db.Orders
                .FirstOrDefaultAsync(x =>
                    x.OrderId == orderId &&
                    x.TenantId == session.TenantId &&
                    x.ShopId == session.ShopId &&
                    x.SessionId == session.SessionId &&
                    x.OrderStatus == "Created");

            if (order == null)
            {
                return NotFound(new
                {
                    message = "Order not found or does not belong to this session."
                });
            }

            // 6. Validate File
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Please select a file."
                });
            }

            // 7. Validate File Size
            if (file.Length > MaxFileSize)
            {
                return BadRequest(new
                {
                    message = "Maximum file size allowed is 25 MB."
                });
            }

            // 8. Validate Extension
            string extension =
                Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(new
                {
                    message = "Only PDF, DOC, DOCX, JPG, JPEG and PNG files are allowed."
                });
            }

            // 9. Validate Actual File Signature
            if (!await IsValidFileSignature(file, extension))
            {
                return BadRequest(new
                {
                    message = "Invalid file content. Please upload a valid file."
                });
            }

            // 10. Generate Unique File Name
            string storedFileName = $"{Guid.NewGuid():N}{extension}";

            // 11. Create Tenant Folder
            string folderPath = Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "TenantFiles",
                session.TenantId.ToString()
            );

            Directory.CreateDirectory(folderPath);

            fullPath = Path.Combine(folderPath, storedFileName);

            // 12. Save Physical File
            using (var stream = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true))
            {
                await file.CopyToAsync(stream);
            }

            string storageKey =
                $"TenantFiles/{session.TenantId}/{storedFileName}";

            // 13. Create OrderFile Database Record
            var orderFile = new OrderFile
            {
                OrderId = order.OrderId,
                OriginalFileName = Path.GetFileName(file.FileName),
                StoredFileName = storedFileName,
                StoragePath = storageKey,
                ContentType = file.ContentType ?? "application/octet-stream",
                FileExtension = extension,
                FileSizeBytes = file.Length,
                PageCount = null,
                UploadStatus = "Uploaded",
                UploadedAtUtc = DateTime.UtcNow
            };

            _db.OrderFiles.Add(orderFile);

            await _db.SaveChangesAsync();

            // 14. Return Response
            return Ok(new
            {
                message = "Document uploaded and saved successfully.",

                orderFile = new
                {
                    orderFileId = orderFile.OrderFileId,
                    orderId = orderFile.OrderId,
                    orderNumber = order.OrderNumber,
                    sessionId = session.SessionId,
                    tenantId = session.TenantId,
                    shopId = session.ShopId,
                    originalFileName = orderFile.OriginalFileName,
                    storedFileName = orderFile.StoredFileName,
                    contentType = orderFile.ContentType,
                    fileExtension = orderFile.FileExtension,
                    fileSizeBytes = orderFile.FileSizeBytes,
                    pageCount = orderFile.PageCount,
                    uploadStatus = orderFile.UploadStatus,
                    storagePath = orderFile.StoragePath,
                    uploadedAtUtc = orderFile.UploadedAtUtc
                }
            });
        }
        catch (Exception ex)
        {
            // Remove physical file if database save failed
            if (fullPath != null && System.IO.File.Exists(fullPath))
            {
                try
                {
                    System.IO.File.Delete(fullPath);
                }
                catch
                {
                    // Log cleanup failure in production
                }
            }

            return StatusCode(500, new
            {
                message = "Document upload failed.",
                error = ex.Message
            });
        }
    }

    private static async Task<bool> IsValidFileSignature(
        IFormFile file,
        string extension)
    {
        byte[] buffer = new byte[8];

        using (var stream = file.OpenReadStream())
        {
            int bytesRead = await stream.ReadAsync(
                buffer.AsMemory(0, buffer.Length));

            if (bytesRead < 2)
                return false;

            switch (extension)
            {
                case ".pdf":
                    return bytesRead >= 5 &&
                        buffer[0] == 0x25 &&
                        buffer[1] == 0x50 &&
                        buffer[2] == 0x44 &&
                        buffer[3] == 0x46 &&
                        buffer[4] == 0x2D;

                case ".jpg":
                case ".jpeg":
                    return bytesRead >= 3 &&
                        buffer[0] == 0xFF &&
                        buffer[1] == 0xD8 &&
                        buffer[2] == 0xFF;

                case ".png":
                    return bytesRead >= 8 &&
                        buffer[0] == 0x89 &&
                        buffer[1] == 0x50 &&
                        buffer[2] == 0x4E &&
                        buffer[3] == 0x47 &&
                        buffer[4] == 0x0D &&
                        buffer[5] == 0x0A &&
                        buffer[6] == 0x1A &&
                        buffer[7] == 0x0A;

                case ".doc":
                    return bytesRead >= 8 &&
                        buffer[0] == 0xD0 &&
                        buffer[1] == 0xCF &&
                        buffer[2] == 0x11 &&
                        buffer[3] == 0xE0 &&
                        buffer[4] == 0xA1 &&
                        buffer[5] == 0xB1 &&
                        buffer[6] == 0x1A &&
                        buffer[7] == 0xE1;

                case ".docx":
                    return bytesRead >= 4 &&
                        buffer[0] == 0x50 &&
                        buffer[1] == 0x4B &&
                        buffer[2] == 0x03 &&
                        buffer[3] == 0x04;

                default:
                    return false;
            }
        }
    }
}
