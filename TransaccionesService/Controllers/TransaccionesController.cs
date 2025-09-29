using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransaccionesService.Data;
using TransaccionesService.Models;
using System.Net.Http.Json;

namespace TransaccionesService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransaccionesController : ControllerBase
{
    private readonly TransaccionesContext _context;
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public TransaccionesController(TransaccionesContext context, IHttpClientFactory httpFactory, IConfiguration config)
    {
        _context = context;
        _http = httpFactory.CreateClient();
        _config = config;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Transaccion>>> GetTransacciones(int page = 0, int pageSize = 10)
    {
        var query = _context.Transacciones.AsQueryable();
    
        var total = await query.CountAsync();
    
        var items = await query
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();
    
        return Ok(new { items, total });
    } 

    [HttpGet("{id}")]
    public async Task<ActionResult<Transaccion>> GetTransaccion(int id)
    {
        var tx = await _context.Transacciones.FindAsync(id);
        return tx is null ? NotFound() : tx;
    }

    [HttpPost]
    public async Task<ActionResult<Transaccion>> CreateTransaccion(Transaccion tx)
    {
        string productosUrl = _config["Services:Productos"]!;
        var producto = await _http.GetFromJsonAsync<ProductoDto>($"{productosUrl}/{tx.ProductoId}");
        if (producto is null)
            return BadRequest("Producto no encontrado");

        int stockActual = producto.Stock;

        if (tx.Tipo.ToLower() == "venta" && tx.Cantidad > stockActual)
            return BadRequest("Stock insuficiente");

        tx.Fecha = TimeZoneInfo.ConvertTimeFromUtc(tx.Fecha.ToUniversalTime(),
            TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"));
        tx.PrecioTotal = tx.Cantidad * tx.PrecioUnitario;

        _context.Transacciones.Add(tx);
        await _context.SaveChangesAsync();

        stockActual = tx.Tipo.ToLower() == "compra"
            ? stockActual + tx.Cantidad
            : stockActual - tx.Cantidad;

        producto.Stock = stockActual;

        var response = await _http.PutAsJsonAsync($"{productosUrl}/{tx.ProductoId}", producto);
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Error actualizando producto en ProductosService");

        return CreatedAtAction(nameof(GetTransaccion), new { id = tx.Id }, tx);
    }

    [HttpGet("historial/{productoId}")]
    public async Task<ActionResult<IEnumerable<Transaccion>>> GetHistorial(
    int productoId,
    [FromQuery] DateTime? fechaInicio,
    [FromQuery] DateTime? fechaFin,
    [FromQuery] string? tipo)
    {
        var query = _context.Transacciones.AsQueryable();

        query = query.Where(t => t.ProductoId == productoId);

        if (fechaInicio.HasValue)
            query = query.Where(t => t.Fecha >= fechaInicio.Value);

        if (fechaFin.HasValue)
            query = query.Where(t => t.Fecha <= fechaFin.Value);

        if (!string.IsNullOrEmpty(tipo))
            query = query.Where(t => t.Tipo.ToLower() == tipo.ToLower());

        var historial = await query.ToListAsync();

        return Ok(historial);
    }


}
