namespace TurnosApp.Core.Entities;

public class RolUsuario
{
    public int Id { get; set; }
    public string Descripcion { get; set; }

    public List<Empleado>? Empleados { get; set; }
}