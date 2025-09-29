using Microsoft.EntityFrameworkCore;
using ProductosService.Models;

namespace ProductosService.Data;

public class ProductosContext : DbContext
{
    public ProductosContext(DbContextOptions<ProductosContext> options) : base(options) { }

    public DbSet<Producto> Productos { get; set; }
}
