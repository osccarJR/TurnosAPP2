using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TurnosApp.Core.Entities;
using TurnosApp.Core.Services;
using TurnosApp.Infrastructure.Data;

namespace TurnosApp.Infrastructure.Services
{
    public class AsignacionService : IAsignacionService
    {
        private readonly ApplicationDbContext _context;
        private readonly AsignadorTurnosService _asignadorTurnos;

        public AsignacionService(ApplicationDbContext context)
        {
            _context = context;
            _asignadorTurnos = new AsignadorTurnosService();
        }

        public async Task GenerarAsignacionesAsync()
        {
            var hoy = DateTime.Now.Date;
            var inicioSemana = hoy.AddDays(-(int)hoy.DayOfWeek + (int)DayOfWeek.Monday);

            var empleados = await _context.Empleados
                .Include(e => e.Disponibilidades)
                .ToListAsync();

            var turnos = await _context.Turnos.ToListAsync();

            var asignacionesExistentes = await _context.TurnosAsignados
                .Where(a => a.Fecha >= inicioSemana)
                .ToListAsync();

            var nuevasAsignaciones = _asignadorTurnos.GenerarAsignaciones(
                empleados,
                turnos,
                inicioSemana,
                asignacionesExistentes
            );

            _context.TurnosAsignados.AddRange(nuevasAsignaciones);
            await _context.SaveChangesAsync();
        }

        public async Task ReiniciarSemanaAsync()
        {
            _context.TurnosAsignados.RemoveRange(_context.TurnosAsignados);
            _context.LogsAsignaciones.RemoveRange(_context.LogsAsignaciones);  // Cambio realizado aquí
            _context.Disponibilidades.RemoveRange(_context.Disponibilidades);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsignacionAsync(int asignacionId)
        {
            var asignacion = await _context.TurnosAsignados.FindAsync(asignacionId);
            if (asignacion != null)
            {
                _context.TurnosAsignados.Remove(asignacion);
                await _context.SaveChangesAsync();
            }
        }
    }
}
