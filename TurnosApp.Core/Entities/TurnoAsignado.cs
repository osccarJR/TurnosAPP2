namespace TurnosApp.Core.Entities;

public class TurnoAsignado
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }

    public int TurnoId { get; set; }
    public Turno Turno { get; set; }

    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; }
}