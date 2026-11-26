
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;
using System.Globalization;

namespace Smart_Xerox_Centre.Controllers;

[ApiController]
public abstract class CrudController<TEntity, TKey> : ControllerBase
    where TEntity : class
{
    protected readonly ApplicationDbContext Db;
    protected readonly DbSet<TEntity> Set;

    protected CrudController(ApplicationDbContext db)
    {
        Db = db;
        Set = db.Set<TEntity>();
    }

    protected abstract string KeyPropertyName { get; }

    private object ConvertKey(TKey id)
    {
        var property = Db.Model.FindEntityType(typeof(TEntity))?
            .FindPrimaryKey()?.Properties.SingleOrDefault();

        if (property == null)
            throw new InvalidOperationException(
                $"{typeof(TEntity).Name} must have one primary key.");

        var targetType =
            Nullable.GetUnderlyingType(property.ClrType)
            ?? property.ClrType;

        if (targetType == typeof(Guid))
            return Guid.Parse(id!.ToString()!);

        return Convert.ChangeType(
            id, targetType, CultureInfo.InvariantCulture)!;
    }

    [HttpGet]
    public virtual async Task<ActionResult<IEnumerable<TEntity>>> GetAll()
    {
        var data = await Set.AsNoTracking().ToListAsync();
        return Ok(data);
    }

    [HttpGet("{id}")]
    public virtual async Task<ActionResult<TEntity>> GetById(TKey id)
    {
        var key = ConvertKey(id);
        var entity = await Set.FindAsync(key);

        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public virtual async Task<ActionResult<TEntity>> Create(
        [FromBody] TEntity entity)
    {
        if (entity == null)
            return BadRequest("Request body is required.");

        // Validate only scalar and FK properties.
        // Navigation properties are not required in request JSON.
        var entityType = Db.Model.FindEntityType(typeof(TEntity));

        if (entityType == null)
            return BadRequest("Entity mapping not found.");

        foreach (var navigation in entityType.GetNavigations())
        {
            var navigationEntry = Db.Entry(entity).Navigation(navigation.Name);
            navigationEntry.IsModified = false;

            if (navigationEntry.CurrentValue != null)
                navigationEntry.CurrentValue = null;
        }

        Set.Add(entity);

        await Db.SaveChangesAsync();

        var key = Db.Entry(entity)
            .Property(KeyPropertyName).CurrentValue;

        return CreatedAtAction(
            nameof(GetById),
            new { id = key },
            entity);
    }

    [HttpPut("{id}")]
    public virtual async Task<IActionResult> Update(
        TKey id, [FromBody] TEntity entity)
    {
        if (entity == null)
            return BadRequest("Request body is required.");

        var key = ConvertKey(id);
        var existing = await Set.FindAsync(key);

        if (existing == null)
            return NotFound();

        Db.Entry(existing).CurrentValues.SetValues(entity);

        await Db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(TKey id)
    {
        var key = ConvertKey(id);
        var entity = await Set.FindAsync(key);

        if (entity == null)
            return NotFound();

        Set.Remove(entity);

        await Db.SaveChangesAsync();

        return NoContent();
    }
}
