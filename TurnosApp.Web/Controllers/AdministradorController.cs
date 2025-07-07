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
        private readonly IAsignacionService _asignacionService;

        public AdministradorController(ApplicationDbContext context, IAsignacionService asignacionService)
        {
            _context = context;
            _asignacionService = asignacionService;
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

            await _asignacionService.GenerarAsignacionesAsync();

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

            // Refactorización: Llamar al servicio para eliminar la asignación
            await _asignacionService.EliminarAsignacionAsync(id);

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
        public async Task<IActionResult> ReiniciarSemana()
        {
            if (HttpContext.Session.GetString("Rol") != "Administrador")
                return RedirectToAction("Login", "Account");

            await _asignacionService.ReiniciarSemanaAsync();

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
