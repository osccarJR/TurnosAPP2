namespace TurnosApp.Core.Entities;

public class Disponibilidad
{
    public int Id { get; set; }
    public string DiaSemana { get; set; } = string.Empty;

    public int TurnoId { get; set; }
    public Turno Turno { get; set; }

    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; }
}