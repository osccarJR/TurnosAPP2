namespace TurnosApp.Core.Entities;

public class Empleado
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Correo { get; set; }

    public int RolId { get; set; }
    public RolUsuario Rol { get; set; }

    public List<Disponibilidad>? Disponibilidades { get; set; }
    public List<TurnoAsignado>? TurnosAsignados { get; set; }
}