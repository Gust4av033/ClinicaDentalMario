namespace ClinicaDentalMario.Models
{
    public class TratamientoPacienteModel
    {
        public int Id { get; set; }
        public int IdPaciente { get; set; }
        public int IdDoctor { get; set; }
        public int IdTratamiento { get; set; }

        // Datos descriptivos cargados por JOIN para la interfaz.
        public string? NombrePaciente { get; set; }
        public string? NombreDoctor { get; set; }
        public string? EspecialidadDoctor { get; set; }
        public string? NombreTratamiento { get; set; }
        public string? DescripcionTratamiento { get; set; }

        public decimal CostoTotal { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public string? Observaciones { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        // Propiedades de compatibilidad utilizadas por otras pantallas existentes.
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Especialidad { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        public string? NumeroJVPO { get; set; }
        public bool Activo { get; set; }

        public bool EstaPendiente =>
            string.Equals(Estado, "Pendiente", StringComparison.OrdinalIgnoreCase);

        public bool EstaEnProgreso =>
            string.Equals(Estado, "En progreso", StringComparison.OrdinalIgnoreCase);

        public bool EstaFinalizado =>
            string.Equals(Estado, "Finalizado", StringComparison.OrdinalIgnoreCase);

        public bool EstaCancelado =>
            string.Equals(Estado, "Cancelado", StringComparison.OrdinalIgnoreCase);

        public bool PuedeEditar => EstaPendiente || EstaEnProgreso;
        public bool PuedeIniciar => EstaPendiente;
        public bool PuedeFinalizar => EstaEnProgreso;
        public bool PuedeCancelar => EstaPendiente || EstaEnProgreso;
    }
}
