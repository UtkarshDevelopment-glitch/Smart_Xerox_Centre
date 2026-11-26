using Microsoft.AspNetCore.Mvc;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Models;

namespace Smart_Xerox_Centre.Controllers;

[Route("api/[controller]")]
public class PaymentsController : CrudController<Payment, long>
{
    public PaymentsController(ApplicationDbContext db) : base(db) { }
    protected override string KeyPropertyName => nameof(Payment.PaymentId);
}
