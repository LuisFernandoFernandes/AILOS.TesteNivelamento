using Dapper;
using MediatR;
using Microsoft.Data.Sqlite;
using Questao5.Application.Queries.Requests;
using Questao5.Application.Queries.Responses;
using Questao5.Domain.Entities;
using Questao5.Domain.Exceptions;
using Questao5.Infrastructure.Sqlite;

namespace Questao5.Application.Handlers
{
    public class ConsultarSaldoQueryHandler : IRequestHandler<ConsultarSaldoQuery, SaldoResponse>
    {
        private readonly DatabaseConfig _config;

        public ConsultarSaldoQueryHandler(DatabaseConfig config)
        {
            _config = config;
        }

        public async Task<SaldoResponse> Handle(ConsultarSaldoQuery request, CancellationToken cancellationToken)
        {
            using var connection = new SqliteConnection(_config.Name);

            var idContaCorrente = request.IdContaCorrente;

            var conta = await GetContaCorrente(connection, idContaCorrente);

            var saldo = await GetSaldo(connection, idContaCorrente);

            return new SaldoResponse
            {
                NumeroConta = conta.Numero,
                NomeTitular = conta.Nome,
                DataConsulta = DateTime.Now,
                Saldo = saldo
            };
        }

        private async Task<ContaCorrenteDto> GetContaCorrente(SqliteConnection connection, string idContaCorrente)
        {
            var conta = await connection.QueryFirstOrDefaultAsync<ContaCorrenteDto>(
                "SELECT * FROM contacorrente WHERE idcontacorrente = @id",
                new { id = idContaCorrente });

            if (conta == null)
                throw new BusinessException("INVALID_ACCOUNT", "A conta informada não existe.");

            if (conta.Ativo != 1)
                throw new BusinessException("INACTIVE_ACCOUNT", "A conta está inativa.");

            return conta;
        }


        private async Task<double> GetSaldo(SqliteConnection connection, string idContaCorrente)
        {
            var creditos = await SomaPorTipoMovimento("C", connection, idContaCorrente);

            var debitos = await SomaPorTipoMovimento("D", connection, idContaCorrente);

            return creditos - debitos;
        }

        private async Task<double> SomaPorTipoMovimento(string tipoMovimento, SqliteConnection connection, string idContaCorrente)
        {
            return await connection.ExecuteScalarAsync<double>(
                 "SELECT IFNULL(SUM(valor), 0) FROM movimento WHERE idcontacorrente = @id AND tipomovimento = @tipo",
                 new { id = idContaCorrente, tipo = tipoMovimento });
        }
    }
}
