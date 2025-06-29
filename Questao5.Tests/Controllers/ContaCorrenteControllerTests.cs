using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Questao5.Application.Commands.Requests;
using Questao5.Application.Queries.Requests;
using Questao5.Application.Queries.Responses;
using Questao5.Domain.Exceptions;
using Questao5.Infrastructure.Services.Controllers;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Questao5.Tests.Controllers
{
    public class ContaCorrenteControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly ContaCorrenteController _controller;

        public ContaCorrenteControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _controller = new ContaCorrenteController(_mediatorMock.Object);
        }

        [Fact]
        public async Task Movimentar_DeveRetornar200_QuandoSucesso()
        {
            var command = new CriarMovimentacaoCommand
            {
                IdContaCorrente = "123",
                Valor = 100,
                TipoMovimento = "C",
                Idempotencia = "abc"
            };

            _mediatorMock.Setup(m => m.Send(It.IsAny<CriarMovimentacaoCommand>(), default)).ReturnsAsync("id123");

            var result = await _controller.Movimentar(command) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            var response = result.Value.GetType().GetProperty("IdMovimento").GetValue(result.Value, null);
            Assert.Equal("id123", response);
        }

        [Fact]
        public async Task Movimentar_DeveRetornar400_QuandoBusinessException()
        {
            var command = new CriarMovimentacaoCommand();

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<CriarMovimentacaoCommand>(), default))
                .ThrowsAsync(new BusinessException("INVALID_TYPE", "Tipo inválido."));

            var result = await _controller.Movimentar(command) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);

            var responseTipo = result.Value.GetType().GetProperty("tipo").GetValue(result.Value, null);
            var responseMensagem = result.Value.GetType().GetProperty("mensagem").GetValue(result.Value, null);

            Assert.Equal("INVALID_TYPE", responseTipo);
            Assert.Equal("Tipo inválido.", responseMensagem);
        }

        [Fact]
        public async Task Movimentar_DeveRetornar500_QuandoErroInesperado()
        {
            var command = new CriarMovimentacaoCommand();

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<CriarMovimentacaoCommand>(), default))
                .ThrowsAsync(new Exception("Falha interna"));

            var result = await _controller.Movimentar(command) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);

            var responseTipo = result.Value.GetType().GetProperty("tipo").GetValue(result.Value, null);
            Assert.Equal("INTERNAL_ERROR", responseTipo);
        }

        [Fact]
        public async Task ObterSaldo_DeveRetornar200_QuandoSucesso()
        {
            var query = new ConsultarSaldoQuery { IdContaCorrente = "abc" };
            var response = new SaldoResponse
            {
                NumeroConta = 123,
                NomeTitular = "João",
                Saldo = 100,
                DataConsulta = DateTime.Now
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<ConsultarSaldoQuery>(), default))
                .ReturnsAsync(response);

            var result = await _controller.ObterSaldo("abc") as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            var responseValue = Assert.IsType<SaldoResponse>(result.Value);
            Assert.Equal(123, responseValue.NumeroConta);
        }

        [Fact]
        public async Task ObterSaldo_DeveRetornar400_QuandoBusinessException()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<ConsultarSaldoQuery>(), default))
                .ThrowsAsync(new BusinessException("INVALID_ACCOUNT", "Conta inválida."));

            var result = await _controller.ObterSaldo("xyz") as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);

            var responseTipo = result.Value.GetType().GetProperty("tipo").GetValue(result.Value, null);
            Assert.Equal("INVALID_ACCOUNT", responseTipo);
        }

        [Fact]
        public async Task ObterSaldo_DeveRetornar500_QuandoErroInesperado()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<ConsultarSaldoQuery>(), default))
                .ThrowsAsync(new Exception("Erro inesperado"));

            var result = await _controller.ObterSaldo("abc") as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(500, result.StatusCode);

            var responseTipo = result.Value.GetType().GetProperty("tipo").GetValue(result.Value, null);
            Assert.Equal("INTERNAL_ERROR", responseTipo);
        }
    }
}
