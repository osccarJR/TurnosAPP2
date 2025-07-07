namespace TurnosApp.Core.Entities;

public class Turno
{
    public int Id { get; set; }
    public string Nombre { get; set; }

    public List<TurnoAsignado>? TurnosAsignados { get; set; }
}