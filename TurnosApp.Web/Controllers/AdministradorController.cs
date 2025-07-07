using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurnosApp.Infrastructure.Data;
using TurnosApp.Core.Services;
using TurnosApp.Core.Entities;
using TurnosApp.Core.Helpers;
using TurnosApp.Core.ViewModels;

namespace TurnosApp.Web.Controllers
{
    public class AdministradorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdministradorController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Rol") != "Administrador")
                return RedirectToAction("Login", "Account");

            return View();
        }

        public async Task<IActionResult> GenerarAsignaciones()
        {
            if (HttpContext.Session.GetString("Rol") != "Administrador")
                return RedirectToAction("Login", "Account");

            var hoy = DateTime.Now.Date;
            var inicioSemana = hoy.AddDays(-(int)hoy.DayOfWeek + (int)DayOfWeek.Monday);

            var yaGenerado = await _context.LogsAsignaciones
                .AnyAsync(log => log.FechaEjecucion >= inicioSemana);

            if (yaGenerado)
            {
                TempData["mensajeError"] = "Ya se han generado turnos esta semana.";
                return RedirectToAction("Index");
            }

            var semanaInicio = ObtenerLunesDeEstaSemana();
            var semanaFin = semanaInicio.AddDays(6);

            var empleados = await _context.Empleados
                .Include(e => e.Disponibilidades)
                .ToListAsync();

            var turnos = await _context.Turnos.ToListAsync();
            var dias = new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" };

            int generadosAutomaticamente = 0;

            foreach (var emp in empleados.Where(e => e.Disponibilidades == null || !e.Disponibilidades.Any()))
            {
                var random = new Random();

                foreach (var dia in dias)
                {
                    var turnoRandom = turnos[random.Next(turnos.Count)];
                    _context.Disponibilidades.Add(new Disponibilidad
                    {
                        EmpleadoId = emp.Id,
                        DiaSemana = dia,
                        TurnoId = turnoRandom.Id
                    });
                }

                generadosAutomaticamente++;
            }

            await _context.SaveChangesAsync();

            var existentes = await _context.TurnosAsignados
                .Where(t => t.Fecha >= semanaInicio && t.Fecha <= semanaFin)
                .ToListAsync();

            var asignador = new AsignadorTurnosService();
            var nuevas = asignador.GenerarAsignaciones(empleados, turnos, semanaInicio, existentes);

            _context.TurnosAsignados.AddRange(nuevas);

            _context.LogsAsignaciones.Add(new LogAsignacion
            {
                FechaEjecucion = DateTime.Now,
                Usuario = HttpContext.Session.GetString("Nombre") ?? "Desconocido",
                CantidadAsignaciones = nuevas.Count
            });

            await _context.SaveChangesAsync();

            TempData["mensaje"] = $"Se generaron {nuevas.Count} nuevas asignaciones.";

            if (generadosAutomaticamente > 0)
            {
                TempData["mensaje"] += $" {generadosAutomaticamente} empleado(s) no registraron su disponibilidad, por lo que se les generó aleatoriamente.";
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> VerAsignaciones(DateTime? semanaInicio, int? empleadoId, int? turnoId)
        {
            if (HttpContext.Session.GetString("Rol") != "Administrador")
                return RedirectToAction("Login", "Account");

            var query = _context.TurnosAsignados
                .Include(t => t.Turno)
                .Include(t => t.Empleado)
                .AsQueryable();

            if (semanaInicio.HasValue)
            {
                var inicio = semanaInicio.Value.Date;
                var fin = inicio.AddDays(6);
                query = query.Where(t => t.Fecha >= inicio && t.Fecha <= fin);
            }

            if (empleadoId.HasValue)
                query = query.Where(t => t.EmpleadoId == empleadoId);

            if (turnoId.HasValue)
                query = query.Where(t => t.TurnoId == turnoId);

            var asignaciones = await query
                .OrderBy(t => t.Empleado.Nombre)
                .ThenBy(t => t.Fecha)
                .ToListAsync();

            var asignacionesPorEmpleado = asignaciones
                .GroupBy(a => a.Empleado.Nombre)
                .ToDictionary(g => g.Key, g => g.Count());

            var asignacionesPorDia = asignaciones
                .GroupBy(a => a.Fecha.DayOfWeek)
                .ToDictionary(g => g.Key, g => g.Count());

            var resumenPorTurno = asignaciones
                .GroupBy(a => new { a.Fecha.DayOfWeek, a.Turno.Nombre })
                .ToDictionary(g => (dynamic)new { g.Key.DayOfWeek, g.Key.Nombre }, g => g.Count());

            ViewBag.PorEmpleado = asignacionesPorEmpleado;
            ViewBag.PorDia = asignacionesPorDia;
            ViewBag.ResumenPorTurno = resumenPorTurno;
            ViewBag.Empleados = await _context.Empleados.ToListAsync();
            ViewBag.Turnos = await _context.Turnos.ToListAsync();
            ViewBag.SemanaInicio = semanaInicio?.ToString("yyyy-MM-dd") ?? "";

            return View(asignaciones);
        }

        [HttpPost]
        public async Task<IActionResult> EliminarAsignacion(int id)
        {
            if (HttpContext.Session.GetString("Rol") != "Administrador")
                return RedirectToAction("Login", "Account");

            var asignacion = await _context.TurnosAsignados.FindAsync(id);

            if (asignacion != null)
            {
                _context.TurnosAsignados.Remove(asignacion);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("VerAsignaciones");
        }

        public async Task<IActionResult> VerLogs()
        {
            if (HttpContext.Session.GetString("Rol") != "Administrador")
                return RedirectToAction("Login", "Account");

            var logs = await _context.LogsAsignaciones
                .OrderByDescending(l => l.FechaEjecucion)
                .ToListAsync();

            return View(logs);
        }

        [HttpPost]
        public IActionResult ReiniciarSemana()
        {
            var inicioSemana = DateTime.Now.StartOfWeek(DayOfWeek.Monday);
            var finSemana = inicioSemana.AddDays(7);

            var asignacionesSemana = _context.TurnosAsignados
                .Where(t => t.Fecha >= inicioSemana && t.Fecha < finSemana)
                .ToList();
            _context.TurnosAsignados.RemoveRange(asignacionesSemana);

            var logsSemana = _context.LogsAsignaciones
                .Where(l => l.FechaEjecucion >= inicioSemana && l.FechaEjecucion < finSemana)
                .ToList();
            _context.LogsAsignaciones.RemoveRange(logsSemana);

            var disponibilidades = _context.Disponibilidades.ToList();
            _context.Disponibilidades.RemoveRange(disponibilidades);

            _context.SaveChanges();

            TempData["mensaje"] = "Se reinició correctamente la semana. Las disponibilidades fueron eliminadas, y los empleados deberán volver a registrarlas.";
            return RedirectToAction("Index");
        }

        private DateTime ObtenerLunesDeEstaSemana()
        {
            var hoy = DateTime.Now;
            int diasDesdeLunes = (int)hoy.DayOfWeek - (int)DayOfWeek.Monday;
            return hoy.AddDays(diasDesdeLunes < 0 ? -6 : -diasDesdeLunes).Date;
        }

        public async Task<IActionResult> ReporteTurnos(DateTime? fechaInicio, DateTime? fechaFin)
        {
            if (HttpContext.Session.GetString("Rol") != "Administrador")
                return RedirectToAction("Login", "Account");

            var query = _context.TurnosAsignados
                .Include(t => t.Empleado)
                .AsQueryable();

            if (fechaInicio.HasValue)
                query = query.Where(t => t.Fecha >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(t => t.Fecha <= fechaFin.Value);

            var resultado = await query
                .GroupBy(t => new { t.Empleado.Nombre, t.Empleado.Correo })
                .Select(g => new ReporteEmpleadoTurnosViewModel
                {
                    Nombre = g.Key.Nombre,
                    Correo = g.Key.Correo,
                    CantidadTurnos = g.Count()
                })
                .OrderByDescending(r => r.CantidadTurnos)
                .ToListAsync();

            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");

            return View(resultado);
        }
    }
}
