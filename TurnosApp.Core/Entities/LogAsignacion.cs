namespace TurnosApp.Core.Entities
{
    public class LogAsignacion
    {
        public int Id { get; set; }
        public DateTime FechaEjecucion { get; set; }
        public string Usuario { get; set; } = "";
        public int CantidadAsignaciones { get; set; }
    }
}