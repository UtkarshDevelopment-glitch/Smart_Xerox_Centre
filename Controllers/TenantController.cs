
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Models;
using System.ComponentModel.DataAnnotations;

namespace Smart_Xerox_Centre.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TenantController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TenantController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/Tenant
        [HttpGet]
        public async Task<IActionResult> GetAllTenants()
        {
            var tenants = await _context.Tenants
                .AsNoTracking()
                .Select(t => new TenantResponse
                {
                    TenantId = t.TenantId,
                    TenantCode = t.TenantCode,
                    TenantName = t.TenantName,
                    Email = t.Email,
                    Phone = t.Phone,
                    Status = t.Status,
                    CreatedAtUtc = t.CreatedAtUtc,
                    UpdatedAtUtc = t.UpdatedAtUtc
                })
                .ToListAsync();

            return Ok(tenants);
        }

        // 2. GET: api/Tenant/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetTenantById(long id)
        {
            var tenant = await _context.Tenants
                .AsNoTracking()
                .Where(t => t.TenantId == id)
                .Select(t => new TenantResponse
                {
                    TenantId = t.TenantId,
                    TenantCode = t.TenantCode,
                    TenantName = t.TenantName,
                    Email = t.Email,
                    Phone = t.Phone,
                    Status = t.Status,
                    CreatedAtUtc = t.CreatedAtUtc,
                    UpdatedAtUtc = t.UpdatedAtUtc
                })
                .FirstOrDefaultAsync();

            if (tenant == null)
            {
                return NotFound(new
                {
                    Message = "Tenant not found."
                });
            }

            return Ok(tenant);
        }

        // 3. POST: api/Tenant
        [HttpPost]
        public async Task<IActionResult> CreateTenant(
            [FromBody] CreateTenantRequest request)
        {
            var tenant = new Tenant
            {
                TenantCode = request.TenantCode,
                TenantName = request.TenantName,
                Email = request.Email,
                Phone = request.Phone,
                Status = "Active",
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            var response = new TenantResponse
            {
                TenantId = tenant.TenantId,
                TenantCode = tenant.TenantCode,
                TenantName = tenant.TenantName,
                Email = tenant.Email,
                Phone = tenant.Phone,
                Status = tenant.Status,
                CreatedAtUtc = tenant.CreatedAtUtc,
                UpdatedAtUtc = tenant.UpdatedAtUtc
            };

            return CreatedAtAction(
                nameof(GetTenantById),
                new { id = tenant.TenantId },
                response
            );
        }

        // 4. PUT: api/Tenant/1
        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateTenant(
            long id,
            [FromBody] UpdateTenantRequest request)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.TenantId == id);

            if (tenant == null)
            {
                return NotFound(new
                {
                    Message = "Tenant not found."
                });
            }

            tenant.TenantCode = request.TenantCode;
            tenant.TenantName = request.TenantName;
            tenant.Email = request.Email;
            tenant.Phone = request.Phone;
            tenant.Status = request.Status;
            tenant.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Tenant updated successfully."
            });
        }

        // 5. DELETE: api/Tenant/1
        // Soft Delete - Actual database record will not be deleted.
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteTenant(long id)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.TenantId == id);

            if (tenant == null)
            {
                return NotFound(new
                {
                    Message = "Tenant not found."
                });
            }

            tenant.Status = "Inactive";
            tenant.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Tenant deactivated successfully."
            });
        }
    }

    public class CreateTenantRequest
    {
        [Required]
        [StringLength(50)]
        public string TenantCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string TenantName { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(256)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }
    }

    public class UpdateTenantRequest
    {
        [Required]
        [StringLength(50)]
        public string TenantCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string TenantName { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(256)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";
    }

    public class TenantResponse
    {
        public long TenantId { get; set; }
        public string TenantCode { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
