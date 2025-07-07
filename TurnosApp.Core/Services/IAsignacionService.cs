using System.Threading.Tasks;

namespace TurnosApp.Core.Services
{
    public interface IAsignacionService
    {
        Task GenerarAsignacionesAsync();
        Task ReiniciarSemanaAsync();
        Task EliminarAsignacionAsync(int asignacionId);
    }
}
