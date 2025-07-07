using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurnosApp.Infrastructure.Data;

namespace TurnosApp.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TurnosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TurnosApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("empleados")]
        public async Task<IActionResult> GetEmpleados()
        {
            var empleados = await _context.Empleados.ToListAsync();
            return Ok(empleados);
        }

        [HttpGet("turnos")]
        public async Task<IActionResult> GetTurnos()
        {
            var turnos = await _context.Turnos.ToListAsync();
            return Ok(turnos);
        }

        [HttpGet("asignaciones")]
        public async Task<IActionResult> GetAsignaciones()
        {
            var asignaciones = await _context.TurnosAsignados
                .Include(a => a.Empleado)
                .Include(a => a.Turno)
                .ToListAsync();

            return Ok(asignaciones);
        }
    }
}
