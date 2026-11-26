using Microsoft.AspNetCore.Mvc;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Models;

namespace Smart_Xerox_Centre.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : CrudController<Smart_Xerox_Centre.Models.User, long>
{
    public UsersController(ApplicationDbContext db) : base(db)
    {
    }

    protected override string KeyPropertyName =>
        nameof(Models.User.UserId);
}