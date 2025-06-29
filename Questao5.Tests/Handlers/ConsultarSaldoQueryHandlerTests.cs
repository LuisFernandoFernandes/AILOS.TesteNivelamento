using Dapper;
using Microsoft.Data.Sqlite;
using Questao5.Application.Handlers;
using Questao5.Application.Queries.Requests;
using Questao5.Domain.Exceptions;
using Questao5.Infrastructure.Sqlite;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Questao5.Tests.Handlers
{
    public class ConsultarSaldoQueryHandlerTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ConsultarSaldoQueryHandler _handler;

        public ConsultarSaldoQueryHandlerTests()
        {
            _connection = new SqliteConnection("Data Source=file:memdb1?mode=memory&cache=shared");
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
                    valor REAL,
                    tipomovimento TEXT
                );

                INSERT INTO contacorrente VALUES ('1', 123, 'Luis', 1);
                INSERT INTO movimento VALUES ('mov1', '1', 150.0, 'C');
                INSERT INTO movimento VALUES ('mov2', '1', 50.0, 'D');
            ");

            _handler = new ConsultarSaldoQueryHandler(config);
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        [Fact]
        public async Task Handle_DeveRetornarSaldoCorreto_QuandoSucesso()
        {
            var query = new ConsultarSaldoQuery { IdContaCorrente = "1" };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(123, result.NumeroConta);
            Assert.Equal("Luis", result.NomeTitular);
            Assert.Equal(100.0, result.Saldo);
        }

        [Fact]
        public async Task Handle_DeveLancarExcecao_QuandoContaNaoExiste()
        {
            var query = new ConsultarSaldoQuery { IdContaCorrente = "999" };

            var exception = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(query, CancellationToken.None));

            Assert.Equal("INVALID_ACCOUNT", exception.Tipo);
            Assert.Equal("A conta informada não existe.", exception.Message);
        }

        [Fact]
        public async Task Handle_DeveLancarExcecao_QuandoContaInativa()
        {
            using var conn = new SqliteConnection(_connection.ConnectionString);
            conn.Open();
            conn.Execute("INSERT INTO contacorrente VALUES ('2', 456, 'Ana', 0);");

            var query = new ConsultarSaldoQuery { IdContaCorrente = "2" };

            var exception = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(query, CancellationToken.None));

            Assert.Equal("INACTIVE_ACCOUNT", exception.Tipo);
            Assert.Equal("A conta está inativa.", exception.Message);
        }

        [Fact]
        public async Task Handle_DeveRetornarZero_QuandoSemMovimentos()
        {
            using var conn = new SqliteConnection(_connection.ConnectionString);
            conn.Open();
            conn.Execute("INSERT INTO contacorrente VALUES ('3', 789, 'Pedro', 1);");

            var query = new ConsultarSaldoQuery { IdContaCorrente = "3" };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(789, result.NumeroConta);
            Assert.Equal("Pedro", result.NomeTitular);
            Assert.Equal(0.0, result.Saldo);
        }
    }
}
