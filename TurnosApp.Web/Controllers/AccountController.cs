using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurnosApp.Infrastructure.Data;

namespace TurnosApp.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string correo)
    {
        var empleado = await _context.Empleados
            .Include(e => e.Rol)
            .FirstOrDefaultAsync(e => e.Correo == correo);

        if (empleado == null)
        {
            ViewBag.Error = "Correo no encontrado.";
            return View();
        }

        // Guardar en sesión
        HttpContext.Session.SetInt32("EmpleadoId", empleado.Id);
        HttpContext.Session.SetString("Nombre", empleado.Nombre);
        HttpContext.Session.SetString("Rol", empleado.Rol.Descripcion);

        // Redireccionar según rol
        if (empleado.Rol.Descripcion == "Administrador")
        {
            return RedirectToAction("Index", "Administrador");
        }
        else
        {
            return RedirectToAction("Index", "Empleado");
        }
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}