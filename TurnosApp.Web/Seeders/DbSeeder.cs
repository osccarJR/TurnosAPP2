using TurnosApp.Core.Entities;
using TurnosApp.Infrastructure.Data;

namespace TurnosApp.Web.Seeders;

public static class DbSeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        if (!context.RolesUsuario.Any())
        {
            context.RolesUsuario.AddRange(
                new RolUsuario { Descripcion = "Administrador" },
                new RolUsuario { Descripcion = "Empleado" }
            );
            context.SaveChanges();
        }

        if (!context.Turnos.Any())
        {
            context.Turnos.AddRange(
                new Turno { Nombre = "Mañana" },
                new Turno { Nombre = "Tarde" },
                new Turno { Nombre = "Noche" }
            );
            context.SaveChanges();
        }

        if (!context.Empleados.Any())
        {
            var rolEmpleado = context.RolesUsuario.First(r => r.Descripcion == "Empleado");

            context.Empleados.AddRange(
                new Empleado { Nombre = "Juan Pérez", Correo = "juan@empresa.com", RolId = rolEmpleado.Id },
                new Empleado { Nombre = "Ana Torres", Correo = "ana@empresa.com", RolId = rolEmpleado.Id },
                new Empleado { Nombre = "Carlos Díaz", Correo = "carlos@empresa.com", RolId = rolEmpleado.Id }
            );
            context.SaveChanges();
        }

        if (!context.Disponibilidades.Any())
        {
            var empleados = context.Empleados.ToList();
            var turnos = context.Turnos.ToList();

            var dias = new[] { "lunes", "martes", "miércoles", "jueves", "viernes" };

            foreach (var empleado in empleados)
            {
                foreach (var dia in dias)
                {
                    context.Disponibilidades.Add(new Disponibilidad
                    {
                        DiaSemana = dia,
                        TurnoId = turnos[new Random().Next(0, turnos.Count)].Id,
                        EmpleadoId = empleado.Id
                    });
                }
            }

            context.SaveChanges();
        }
    }
}
