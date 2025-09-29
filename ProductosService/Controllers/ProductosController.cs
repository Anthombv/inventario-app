using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductosService.Data;
using ProductosService.Models;

namespace ProductosService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly ProductosContext _context;

    public ProductosController(ProductosContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetProductos(int page = 0, int pageSize = 10)
    {
        var query = _context.Productos.AsQueryable();
    
        var total = await query.CountAsync();
    
        var items = await query
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();
    
        return Ok(new { items, total });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Producto>> GetProducto(int id)
    {
        var producto = await _context.Productos.FindAsync(id);
        return producto is null ? NotFound() : producto;
    }

    [HttpPost]
    public async Task<ActionResult<Producto>> CreateProducto(Producto producto)
    {
        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProducto), new { id = producto.Id }, producto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProducto(int id, Producto producto)
    {
        if (id != producto.Id) return BadRequest();
        _context.Entry(producto).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProducto(int id)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto is null) return NotFound();
        _context.Productos.Remove(producto);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    [HttpGet("{id}/historial")]
    public async Task<ActionResult<IEnumerable<TransaccionDto>>> GetHistorial(
    int id,
    [FromQuery] DateTime? fechaInicio,
    [FromQuery] DateTime? fechaFin,
    [FromQuery] string? tipo,
    [FromServices] IHttpClientFactory httpFactory,
    [FromServices] IConfiguration config)
    {
        var http = httpFactory.CreateClient();
        var transaccionesUrl = config["Services:Transacciones"];

        var queryParams = new List<string>();
        if (fechaInicio.HasValue) queryParams.Add($"fechaInicio={fechaInicio.Value:o}");
        if (fechaFin.HasValue) queryParams.Add($"fechaFin={fechaFin.Value:o}");
        if (!string.IsNullOrEmpty(tipo)) queryParams.Add($"tipo={tipo}");

        var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";

        var url = $"{transaccionesUrl}/historial/{id}{query}";

        var historial = await http.GetFromJsonAsync<List<TransaccionDto>>(url);

        return historial ?? new List<TransaccionDto>();
    }


}
