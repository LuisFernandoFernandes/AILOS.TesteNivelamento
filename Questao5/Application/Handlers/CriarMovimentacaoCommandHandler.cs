using Dapper;
using MediatR;
using Microsoft.Data.Sqlite;
using Questao5.Application.Commands.Requests;
using Questao5.Domain.Exceptions;
using Questao5.Infrastructure.Sqlite;
using System.Text.Json;

namespace Questao5.Application.Handlers
{
    public class CriarMovimentacaoCommandHandler : IRequestHandler<CriarMovimentacaoCommand, string>
    {
        private readonly DatabaseConfig _config;

        public CriarMovimentacaoCommandHandler(DatabaseConfig config)
        {
            _config = config;
        }

        public async Task<string> Handle(CriarMovimentacaoCommand request, CancellationToken cancellationToken)
        {
            using var connection = new SqliteConnection(_config.Name);

            var idempotencia = await VerificarIdempotencia(connection, request.Idempotencia);
            if (idempotencia != null)
                return idempotencia;

            await ValidarContaCorrenteAtiva(connection, request.IdContaCorrente);
            ValidarDadosDaRequisicao(request);

            var id = await InserirMovimento(connection, request);
            await SalvarIdempotencia(connection, request.Idempotencia, id, request);

            return id;
        }

        private async Task<string?> VerificarIdempotencia(SqliteConnection connection, string chave)
        {
            return await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT resultado FROM idempotencia WHERE chave_idempotencia = @chave",
                new { chave });
        }

        private async Task ValidarContaCorrenteAtiva(SqliteConnection connection, string idConta)
        {
            var ativo = await connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT ativo FROM contacorrente WHERE idcontacorrente = @id",
                new { id = idConta });

            if (ativo == null)
                throw new BusinessException("INVALID_ACCOUNT", "A conta informada não existe.");

            if (ativo != 1)
                throw new BusinessException("INACTIVE_ACCOUNT", "A conta está inativa.");
        }

        private void ValidarDadosDaRequisicao(CriarMovimentacaoCommand request)
        {
            if (request.Valor <= 0)
                throw new BusinessException("INVALID_VALUE", "O valor da movimentação deve ser maior que zero.");

            if (request.TipoMovimento != "C" && request.TipoMovimento != "D")
                throw new BusinessException("INVALID_TYPE", "O tipo de movimentação deve ser 'C' ou 'D'.");
        }

        private async Task<string> InserirMovimento(SqliteConnection connection, CriarMovimentacaoCommand request)
        {
            var id = Guid.NewGuid().ToString();
            var data = DateTime.Now.ToString("dd/MM/yyyy");

            await connection.ExecuteAsync(
                @"INSERT INTO movimento (idmovimento, idcontacorrente, datamovimento, tipomovimento, valor)
                  VALUES (@id, @idConta, @data, @tipo, @valor)",
                new
                {
                    id,
                    idConta = request.IdContaCorrente,
                    data,
                    tipo = request.TipoMovimento,
                    valor = request.Valor
                });

            return id;
        }

        private async Task SalvarIdempotencia(SqliteConnection connection, string chave, string resultado, CriarMovimentacaoCommand request)
        {
            await connection.ExecuteAsync(
                @"INSERT INTO idempotencia (chave_idempotencia, requisicao, resultado)
                  VALUES (@chave, @req, @res)",
                new
                {
                    chave,
                    req = JsonSerializer.Serialize(request),
                    res = resultado
                });
        }
    }
}
