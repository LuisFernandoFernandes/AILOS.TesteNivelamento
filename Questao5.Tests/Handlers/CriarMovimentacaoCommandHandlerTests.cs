using Dapper;
using Microsoft.Data.Sqlite;
using Questao5.Application.Commands.Requests;
using Questao5.Application.Handlers;
using Questao5.Domain.Exceptions;
using Questao5.Infrastructure.Sqlite;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Questao5.Tests.Handlers
{
    public class CriarMovimentacaoCommandHandlerTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly CriarMovimentacaoCommandHandler _handler;

        public CriarMovimentacaoCommandHandlerTests()
        {
            _connection = new SqliteConnection("Data Source=file:memdb2?mode=memory&cache=shared");
            _connection.Open();

            var config = new DatabaseConfig { Name = _connection.ConnectionString };

            using var setupConnection = new SqliteConnection(_connection.ConnectionString);
            setupConnection.Open();

            setupConnection.Execute(@"
                CREATE TABLE contacorrente (
                    idcontacorrente TEXT PRIMARY KEY,
                    numero INTEGER,
                    nome TEXT,
                    ativo INTEGER
                );

                CREATE TABLE movimento (
                    idmovimento TEXT PRIMARY KEY,
                    idcontacorrente TEXT,
                    datamovimento TEXT,
                    tipomovimento TEXT,
                    valor REAL
                );

                CREATE TABLE idempotencia (
                    chave_idempotencia TEXT PRIMARY KEY,
                    requisicao TEXT,
                    resultado TEXT
                );

                INSERT INTO contacorrente VALUES ('1', 123, 'Luis', 1);
            ");

            _handler = new CriarMovimentacaoCommandHandler(config);
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        [Fact]
        public async Task Handle_DeveCriarMovimentacao_QuandoValido()
        {
            var command = new CriarMovimentacaoCommand
            {
                IdContaCorrente = "1",
                TipoMovimento = "C",
                Valor = 100,
                Idempotencia = "abc123"
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.False(string.IsNullOrWhiteSpace(result));

            var id = _connection.ExecuteScalar<string>("SELECT idmovimento FROM movimento WHERE idmovimento = @id", new { id = result });
            Assert.Equal(result, id);
        }

        [Fact]
        public async Task Handle_DeveRetornarMesmoId_QuandoIdempotente()
        {
            var command = new CriarMovimentacaoCommand
            {
                IdContaCorrente = "1",
                TipoMovimento = "C",
                Valor = 100,
                Idempotencia = "abc123"
            };

            var id1 = await _handler.Handle(command, CancellationToken.None);
            var id2 = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(id1, id2);
        }

        [Fact]
        public async Task Handle_DeveLancarExcecao_QuandoContaInativa()
        {
            using var conn = new SqliteConnection(_connection.ConnectionString);
            conn.Open();
            conn.Execute("INSERT INTO contacorrente VALUES ('2', 456, 'Ana', 0);");

            var command = new CriarMovimentacaoCommand
            {
                IdContaCorrente = "2",
                TipoMovimento = "D",
                Valor = 50,
                Idempotencia = "def456"
            };

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal("INACTIVE_ACCOUNT", ex.Tipo);
        }

        [Fact]
        public async Task Handle_DeveLancarExcecao_QuandoContaNaoExiste()
        {
            var command = new CriarMovimentacaoCommand
            {
                IdContaCorrente = "999",
                TipoMovimento = "D",
                Valor = 50,
                Idempotencia = "ghi789"
            };

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal("INVALID_ACCOUNT", ex.Tipo);
        }

        [Fact]
        public async Task Handle_DeveLancarExcecao_QuandoTipoInvalido()
        {
            var command = new CriarMovimentacaoCommand
            {
                IdContaCorrente = "1",
                TipoMovimento = "X",
                Valor = 100,
                Idempotencia = "inv001"
            };

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal("INVALID_TYPE", ex.Tipo);
        }

        [Fact]
        public async Task Handle_DeveLancarExcecao_QuandoValorZero()
        {
            var command = new CriarMovimentacaoCommand
            {
                IdContaCorrente = "1",
                TipoMovimento = "C",
                Valor = 0,
                Idempotencia = "inv002"
            };

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Equal("INVALID_VALUE", ex.Tipo);
        }

        [Fact]
        public async Task Handle_DeveSalvarRequisicaoSerializadaNaIdempotencia()
        {
            var command = new CriarMovimentacaoCommand
            {
                IdContaCorrente = "1",
                TipoMovimento = "D",
                Valor = 200,
                Idempotencia = "serializacao001"
            };

            var id = await _handler.Handle(command, CancellationToken.None);

            var requisicaoJson = _connection.ExecuteScalar<string>(
                "SELECT requisicao FROM idempotencia WHERE chave_idempotencia = @chave",
                new { chave = command.Idempotencia });

            Assert.False(string.IsNullOrWhiteSpace(requisicaoJson));
            Assert.Contains("\"IdContaCorrente\":\"1\"", requisicaoJson);
            Assert.Contains("\"TipoMovimento\":\"D\"", requisicaoJson);
        }

    }
}
