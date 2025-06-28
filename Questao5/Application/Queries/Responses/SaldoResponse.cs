namespace Questao5.Application.Queries.Responses
{
    public class SaldoResponse
    {
        public int NumeroConta { get; set; }
        public string NomeTitular { get; set; }
        public DateTime DataConsulta { get; set; }
        public double Saldo { get; set; }
    }
}
