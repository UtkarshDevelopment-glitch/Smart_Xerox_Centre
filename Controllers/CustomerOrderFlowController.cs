
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Models;
using Smart_Xerox_Centre.DTOs.Orders;
using System.Security.Cryptography;

namespace Smart_Xerox_Centre.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [AllowAnonymous]
    public class CustomerOrderFlowController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CustomerOrderFlowController(ApplicationDbContext db)
        {
            _db = db;
        }

        // CUSTOMER: Create Order
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder(
            [FromBody] CreateOrderRequest request,
            [FromHeader(Name = "X-Session-Token")] string sessionToken)
        {
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

                // 2. Generate SHA256 Hash
                string tokenHash = Convert.ToHexString(
                    SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(sessionToken)));

                DateTime now = DateTime.UtcNow;

                // 3. Validate Customer Session
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

                // 4. Validate Shop
                var shop = await _db.Shops
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ShopId == session.ShopId &&
                        x.TenantId == session.TenantId &&
                        x.Status == "Active");

                if (shop == null)
                {
                    return NotFound(new
                    {
                        message = "Shop is inactive or unavailable."
                    });
                }

                // 5. Generate Unique Order Number
                string orderNumber =
                    "ORD" +
                    DateTime.UtcNow.ToString("yyyyMMddHHmmss") +
                    RandomNumberGenerator.GetHexString(4);

                // 6. Create Order
                var order = new Order
                {
                    TenantId = session.TenantId,
                    ShopId = session.ShopId,
                    SessionId = session.SessionId,
                    CustomerId = session.CustomerId,

                    OrderNumber = orderNumber,
                    OrderStatus = "Created",
                    CurrencyCode = "INR",

                    SubTotal = 0,
                    TaxAmount = 0,
                    AdditionalCharges = 0,
                    DiscountAmount = 0,
                    TotalAmount = 0,

                    CustomerNote = request.CustomerNote

                    // CreatedAtUtc: SQL default
                    // RowVersion: SQL Server generates automatically
                };

                // 7. Save Order
                _db.Orders.Add(order);

                await _db.SaveChangesAsync();

                // 8. Return Response
                return Ok(new
                {
                    message = "Order created successfully.",

                    order = new
                    {
                        orderId = order.OrderId,
                        orderNumber = order.OrderNumber,
                        tenantId = order.TenantId,
                        shopId = order.ShopId,
                        sessionId = order.SessionId,
                        customerId = order.CustomerId,
                        shopName = shop.ShopName,
                        orderStatus = order.OrderStatus,
                        currencyCode = order.CurrencyCode,
                        subTotal = order.SubTotal,
                        taxAmount = order.TaxAmount,
                        additionalCharges = order.AdditionalCharges,
                        discountAmount = order.DiscountAmount,
                        totalAmount = order.TotalAmount,
                        customerNote = order.CustomerNote,
                        createdAtUtc = order.CreatedAtUtc
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Order creation failed.",
                    error = ex.Message
                });
            }
        }
    }
}
