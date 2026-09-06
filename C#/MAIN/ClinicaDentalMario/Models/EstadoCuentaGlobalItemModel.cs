namespace ClinicaDentalMario.Models
{
    public class EstadoCuentaGlobalItemModel
    {
        public string NombreTratamiento { get; set; } = string.Empty;
        public decimal CostoTotal { get; set; }
        public decimal TotalAbonado { get; set; }
        public string Estado { get; set; } = string.Empty;

        public bool GeneraSaldo =>
            string.Equals(Estado, "Pendiente", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Estado, "En progreso", StringComparison.OrdinalIgnoreCase);

        public decimal SaldoPendiente => GeneraSaldo
            ? Math.Max(0m, CostoTotal - TotalAbonado)
            : 0m;
    }
}
