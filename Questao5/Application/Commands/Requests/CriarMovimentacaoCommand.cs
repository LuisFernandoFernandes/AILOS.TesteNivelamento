using MediatR;

namespace Questao5.Application.Commands.Requests
{
    public class CriarMovimentacaoCommand : IRequest<string>
    {
        public string Idempotencia { get; set; }
        public string IdContaCorrente { get; set; }
        public double Valor { get; set; }
        public string TipoMovimento { get; set; } // 'C' ou 'D'
    }
}