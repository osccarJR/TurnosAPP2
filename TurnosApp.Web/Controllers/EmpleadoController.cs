using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurnosApp.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using TurnosApp.Core.Helpers;
using TurnosApp.Core.Entities;

namespace TurnosApp.Web.Controllers
{
    public class EmpleadoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmpleadoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Página principal del empleado
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("EmpleadoId") == null)
                return RedirectToAction("Login", "Account");

            return View();
        }

        // Consulta de turnos personales
        public async Task<IActionResult> MisTurnos()
        {
            if (HttpContext.Session.GetInt32("EmpleadoId") == null)
                return RedirectToAction("Login", "Account");

            var idEmpleado = HttpContext.Session.GetInt32("EmpleadoId").Value;

            var asignaciones = await _context.TurnosAsignados
                .Include(t => t.Turno)
                .Where(t => t.EmpleadoId == idEmpleado)
                .OrderBy(t => t.Fecha)
                .ToListAsync();

            var empleado = await _context.Empleados.FindAsync(idEmpleado);
            ViewBag.Empleado = empleado?.Nombre;

            return View(asignaciones);
        }

        // Mostrar formulario de disponibilidad
        public async Task<IActionResult> RegistrarDisponibilidad()
        {
            if (HttpContext.Session.GetInt32("EmpleadoId") == null)
                return RedirectToAction("Login", "Account");

            var empleadoId = HttpContext.Session.GetInt32("EmpleadoId").Value;

            var inicioSemana = DateTime.Now.StartOfWeek(DayOfWeek.Monday);

            // Verificar si ya se generaron asignaciones esta semana
            var yaGenerado = await _context.TurnosAsignados
                .AnyAsync(t => t.Fecha >= inicioSemana && t.EmpleadoId == empleadoId);

            if (yaGenerado)
            {
                TempData["Warning"] = "Ya se generaron los turnos esta semana. No puedes modificar tu disponibilidad.";
                return RedirectToAction("Index");
            }

            ViewBag.Turnos = await _context.Turnos.ToListAsync();
            return View();
        }

        // Guardar disponibilidad (POST)
        [HttpPost]
        public async Task<IActionResult> RegistrarDisponibilidad(List<string> dias, int turnoId)
        {
            if (HttpContext.Session.GetInt32("EmpleadoId") == null)
                return RedirectToAction("Login", "Account");

            var idEmpleado = HttpContext.Session.GetInt32("EmpleadoId").Value;

            // Validación: contar cuántos empleados ya eligieron este turno
            var empleadosEnEseTurno = await _context.Disponibilidades
                .Where(d => d.TurnoId == turnoId)
                .Select(d => d.EmpleadoId)
                .Distinct()
                .CountAsync();

            // Mostrar advertencia pero permitir guardar igual
            if (empleadosEnEseTurno >= 2)
            {
                TempData["Warning"] = "Advertencia: Este turno ya fue elegido por varios empleados. Considera elegir otro para una mejor distribución.";
            }

            // Limpiar disponibilidades anteriores del empleado
            var anteriores = _context.Disponibilidades
                .Where(d => d.EmpleadoId == idEmpleado);

            _context.Disponibilidades.RemoveRange(anteriores);
            await _context.SaveChangesAsync();

            // Agregar nuevas disponibilidades
            foreach (var dia in dias)
            {
                _context.Disponibilidades.Add(new Core.Entities.Disponibilidad
                {
                    EmpleadoId = idEmpleado,
                    DiaSemana = dia,
                    TurnoId = turnoId
                });
            }

            await _context.SaveChangesAsync();

            TempData["mensaje"] = "Tu disponibilidad ha sido registrada correctamente.";
            return RedirectToAction("Index");
        }

        // Ver disponibilidad registrada
        public async Task<IActionResult> VerDisponibilidad()
        {
            if (HttpContext.Session.GetInt32("EmpleadoId") == null)
                return RedirectToAction("Login", "Account");

            var idEmpleado = HttpContext.Session.GetInt32("EmpleadoId").Value;

            var disponibilidades = await _context.Disponibilidades
                .Include(d => d.Turno)
                .Where(d => d.EmpleadoId == idEmpleado)
                .OrderBy(d => d.DiaSemana)
                .ToListAsync();

            ViewBag.NombreEmpleado = (await _context.Empleados.FindAsync(idEmpleado))?.Nombre;
            return View(disponibilidades);
        }

        // Ver turnos disponibles para asignación
        public async Task<IActionResult> VerDisponibles()
        {
            var empleadoId = HttpContext.Session.GetInt32("EmpleadoId");
            if (empleadoId == null) return RedirectToAction("Login", "Account");

            var empleado = await _context.Empleados
                .Include(e => e.Disponibilidades)
                .FirstOrDefaultAsync(e => e.Id == empleadoId);

            if (empleado == null || empleado.Disponibilidades == null)
                return View(new List<string>());

            var semanaInicio = ObtenerLunesDeEstaSemana();
            var semanaFin = semanaInicio.AddDays(6);

            // Obtener turnos ya ocupados por otros empleados
            var ocupados = await _context.TurnosAsignados
                .Where(t => t.Fecha >= semanaInicio && t.Fecha <= semanaFin)
                .GroupBy(t => new { t.Fecha, t.TurnoId })
                .Select(g => new { g.Key.Fecha, g.Key.TurnoId })
                .ToListAsync();

            var ocupadosSet = new HashSet<(DateTime, int)>(ocupados.Select(o => (o.Fecha.Date, o.TurnoId)));

            // Obtener asignaciones actuales del empleado para no repetir día
            var asignacionesEmpleado = await _context.TurnosAsignados
                .Where(t => t.EmpleadoId == empleado.Id && t.Fecha >= semanaInicio && t.Fecha <= semanaFin)
                .ToListAsync();

            var diasYaAsignados = asignacionesEmpleado.Select(a => a.Fecha.DayOfWeek).ToHashSet();

            var disponibles = new List<string>();

            foreach (var disp in empleado.Disponibilidades)
            {
                var dia = ConvertirDiaSemana(disp.DiaSemana);
                if (diasYaAsignados.Contains(dia))
                    continue;

                var fecha = semanaInicio.AddDays((int)dia - (int)DayOfWeek.Monday);
                var clave = (fecha.Date, disp.TurnoId);

                if (!ocupadosSet.Contains(clave))
                {
                    var turno = await _context.Turnos.FindAsync(disp.TurnoId);
                    disponibles.Add($"{disp.DiaSemana} - Turno: {turno?.Nombre}");
                }
            }

            return View(disponibles);
        }

        // Obtener lunes de esta semana
        private DateTime ObtenerLunesDeEstaSemana()
        {
            var hoy = DateTime.Now;
            int diasDesdeLunes = (int)hoy.DayOfWeek - (int)DayOfWeek.Monday;
            return hoy.AddDays(diasDesdeLunes < 0 ? -6 : -diasDesdeLunes).Date;
        }

        // Convertir nombre de día a DayOfWeek
        private DayOfWeek ConvertirDiaSemana(string dia)
        {
            return dia.ToLower() switch
            {
                "lunes" => DayOfWeek.Monday,
                "martes" => DayOfWeek.Tuesday,
                "miércoles" => DayOfWeek.Wednesday,
                "miercoles" => DayOfWeek.Wednesday,
                "jueves" => DayOfWeek.Thursday,
                "viernes" => DayOfWeek.Friday,
                _ => throw new ArgumentException("Día inválido")
            };
        }

        // Acción para registrar un turno manualmente
        [HttpPost]
        public async Task<IActionResult> RegistrarTurno(DateTime fecha, int turnoId)
        {
            var empleadoId = HttpContext.Session.GetInt32("EmpleadoId");
            if (empleadoId == null) return RedirectToAction("Login", "Account");

            var semanaInicio = fecha.StartOfWeek(DayOfWeek.Monday);
            var semanaFin = semanaInicio.AddDays(6);

            // Verificar si el turno ya está ocupado ese día por otro empleado
            var yaOcupado = await _context.TurnosAsignados.AnyAsync(t =>
                t.Fecha.Date == fecha.Date && t.TurnoId == turnoId && t.EmpleadoId != empleadoId);

            if (yaOcupado)
            {
                TempData["mensajeError"] = "Este turno ya está ocupado ese día.";
                return RedirectToAction("VerDisponibles");
            }

            // Verificar si el empleado ya tiene un turno asignado ese día
            var yaAsignadoEseDia = await _context.TurnosAsignados.AnyAsync(t =>
                t.EmpleadoId == empleadoId && t.Fecha.Date == fecha.Date);

            if (yaAsignadoEseDia)
            {
                TempData["mensajeError"] = "Ya tienes un turno asignado ese día.";
                return RedirectToAction("VerDisponibles");
            }

            // Verificar si ya tiene 5 turnos asignados esta semana
            var asignacionesSemana = await _context.TurnosAsignados
                .CountAsync(t => t.EmpleadoId == empleadoId && t.Fecha >= semanaInicio && t.Fecha <= semanaFin);

            if (asignacionesSemana >= 5)
            {
                TempData["mensajeError"] = "Ya tienes el máximo de 5 turnos esta semana.";
                return RedirectToAction("VerDisponibles");
            }

            // Todo está OK, registrar turno
            _context.TurnosAsignados.Add(new TurnoAsignado
            {
                EmpleadoId = empleadoId.Value,
                TurnoId = turnoId,
                Fecha = fecha
            });

            await _context.SaveChangesAsync();

            TempData["mensaje"] = "Turno registrado con éxito.";
            return RedirectToAction("VerDisponibles");
        }

        // Acción para ver los turnos asignados esta semana
        public async Task<IActionResult> VerMisTurnos()
        {
            var empleadoId = HttpContext.Session.GetInt32("EmpleadoId");
            if (empleadoId == null) return RedirectToAction("Login", "Account");

            var hoy = DateTime.Today;
            var semanaInicio = hoy.StartOfWeek(DayOfWeek.Monday);
            var semanaFin = semanaInicio.AddDays(6);

            var turnosAsignados = await _context.TurnosAsignados
                .Include(t => t.Turno)
                .Where(t => t.EmpleadoId == empleadoId && t.Fecha >= semanaInicio && t.Fecha <= semanaFin)
                .ToListAsync();

            // Resumen por día de la semana
            var diasAsignados = turnosAsignados
                .GroupBy(t => t.Fecha.DayOfWeek)
                .ToDictionary(g => g.Key, g => g.Count());

            // **Nueva forma de asegurar que ViewBag.DiasAsignados no sea null**
            ViewBag.DiasAsignados = diasAsignados ?? new Dictionary<DayOfWeek, int>();

            ViewBag.TotalTurnos = turnosAsignados.Count;
            ViewBag.SemanaInicio = semanaInicio.ToString("yyyy-MM-dd");

            return View(turnosAsignados);
        }
    }
}