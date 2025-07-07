using TurnosApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnosApp.Core.Services
{
    public class AsignadorTurnosService
    {
        public List<TurnoAsignado> GenerarAsignaciones(
            List<Empleado> empleados,
            List<Turno> turnos,
            DateTime semanaInicio,
            List<TurnoAsignado> asignacionesExistentes)
        {
            var empleadosSoloTrabajadores = empleados
                .Where(e => e.RolId == 2) // Solo empleados
                .ToList();

            var nuevasAsignaciones = new List<TurnoAsignado>();
            var ocupacionPorDiaYTurno = asignacionesExistentes
                .GroupBy(a => (a.Fecha.Date, a.TurnoId))
                .ToDictionary(g => g.Key, g => g.Count());

            Random rng = new();

            foreach (var empleado in empleadosSoloTrabajadores)
            {
                var diasAsignados = new HashSet<DayOfWeek>();
                int turnosAsignados = 0;

                // Primero, asignar en base a disponibilidad
                if (empleado.Disponibilidades != null)
                {
                    var disponibilidades = empleado.Disponibilidades
                        .OrderBy(d => d.DiaSemana)
                        .ToList();

                    foreach (var disponibilidad in disponibilidades)
                    {
                        if (turnosAsignados >= 5)
                            break;

                        var dia = ConvertirDiaSemana(disponibilidad.DiaSemana);
                        if (diasAsignados.Contains(dia))
                            continue;

                        var fecha = semanaInicio.AddDays((int)dia - (int)DayOfWeek.Monday);
                        var claveOcupacion = (fecha.Date, disponibilidad.TurnoId);

                        if (ocupacionPorDiaYTurno.TryGetValue(claveOcupacion, out int ocupados) && ocupados >= 1)
                            continue;

                        bool yaAsignado = asignacionesExistentes.Any(a =>
                            a.EmpleadoId == empleado.Id && a.Fecha.Date == fecha.Date);

                        if (yaAsignado)
                            continue;

                        nuevasAsignaciones.Add(new TurnoAsignado
                        {
                            EmpleadoId = empleado.Id,
                            TurnoId = disponibilidad.TurnoId,
                            Fecha = fecha
                        });

                        ocupacionPorDiaYTurno[claveOcupacion] = 1;
                        diasAsignados.Add(dia);
                        turnosAsignados++;
                    }
                }

                // Completar turnos restantes si tiene menos de 5 asignados
                var diasLaborales = new[] {
                    DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                    DayOfWeek.Thursday, DayOfWeek.Friday
                };

                foreach (var dia in diasLaborales)
                {
                    if (turnosAsignados >= 5)
                        break;

                    if (diasAsignados.Contains(dia))
                        continue;

                    var fecha = semanaInicio.AddDays((int)dia - (int)DayOfWeek.Monday);

                    // Buscar turno aleatorio disponible
                    var turnoDisponible = turnos.FirstOrDefault(t =>
                        !ocupacionPorDiaYTurno.ContainsKey((fecha.Date, t.Id)) ||
                        ocupacionPorDiaYTurno[(fecha.Date, t.Id)] < 1);

                    if (turnoDisponible == null)
                        continue;

                    nuevasAsignaciones.Add(new TurnoAsignado
                    {
                        EmpleadoId = empleado.Id,
                        TurnoId = turnoDisponible.Id,
                        Fecha = fecha
                    });

                    ocupacionPorDiaYTurno[(fecha.Date, turnoDisponible.Id)] = 1;
                    diasAsignados.Add(dia);
                    turnosAsignados++;
                }
            }

            return nuevasAsignaciones;
        }

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
    }
}