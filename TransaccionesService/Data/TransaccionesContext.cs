using Microsoft.EntityFrameworkCore;
using TransaccionesService.Models;

namespace TransaccionesService.Data;

public class TransaccionesContext : DbContext
{
    public TransaccionesContext(DbContextOptions<TransaccionesContext> options) : base(options) { }

    public DbSet<Transaccion> Transacciones { get; set; }
}
