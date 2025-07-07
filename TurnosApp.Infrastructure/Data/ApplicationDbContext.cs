using Microsoft.EntityFrameworkCore;
using TurnosApp.Core.Entities;

namespace TurnosApp.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<RolUsuario> RolesUsuario => Set<RolUsuario>();
    public DbSet<Disponibilidad> Disponibilidades => Set<Disponibilidad>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<LogAsignacion> LogsAsignaciones { get; set; }

    public DbSet<TurnoAsignado> TurnosAsignados => Set<TurnoAsignado>();
}
