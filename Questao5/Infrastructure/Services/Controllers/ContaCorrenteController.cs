using MediatR;
using Microsoft.AspNetCore.Mvc;
using Questao5.Application.Commands.Requests;
using Questao5.Application.Queries.Requests;
using Questao5.Domain.Exceptions;

namespace Questao5.Infrastructure.Services.Controllers
{
    [ApiController]
    [Route("api/conta-corrente")]
    public class ContaCorrenteController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ContaCorrenteController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("movimentacao")]
        public async Task<IActionResult> Movimentar(CriarMovimentacaoCommand command)
        {
            try
            {
                var id = await _mediator.Send(command);
                return Ok(new { IdMovimento = id });
            }
            catch (BusinessException ex)
            {
                return BadRequest(new { tipo = ex.Tipo, mensagem = ex.Message });
            }
            catch
            {
                return StatusCode(500, new { tipo = "INTERNAL_ERROR", mensagem = "Erro inesperado ao processar movimentação." });
            }
        }

        [HttpGet("saldo/{idContaCorrente}")]
        public async Task<IActionResult> ObterSaldo(string idContaCorrente)
        {
            try
            {
                var result = await _mediator.Send(new ConsultarSaldoQuery { IdContaCorrente = idContaCorrente });
                return Ok(result);
            }
            catch (BusinessException ex)
            {
                return BadRequest(new { tipo = ex.Tipo, mensagem = ex.Message });
            }
            catch
            {
                return StatusCode(500, new { tipo = "INTERNAL_ERROR", mensagem = "Erro inesperado ao consultar saldo." });
            }
        }

    }
}
