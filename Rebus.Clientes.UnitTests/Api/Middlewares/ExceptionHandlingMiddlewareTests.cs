using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Rebus.Clientes.Api.Middlewares;
using Rebus.Clientes.Api.Models;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.UnitTests.Api.Middlewares;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int StatusCode, ApiResponse? Body)> InvocarMiddleware(Exception excecao)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw excecao,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.Invoke(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var body = JsonSerializer.Deserialize<ApiResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return (context.Response.StatusCode, body);
    }

    private static async Task<int> InvocarMiddlewareSemExcecao()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.CompletedTask,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.Invoke(context);

        return context.Response.StatusCode;
    }

    [Fact]
    public async Task Invoke_SemExcecao_DevePassarSemAlterarStatus()
    {
        var statusCode = await InvocarMiddlewareSemExcecao();

        statusCode.Should().Be(200);
    }

    [Fact]
    public async Task Invoke_ValidationException_DeveRetornar400ComErros()
    {
        var erros = new List<ValidationFailure>
        {
            new("Nome", "Nome é obrigatório."),
            new("Email", "Email é inválido.")
        };
        var excecao = new ValidationException(erros);

        var (statusCode, body) = await InvocarMiddleware(excecao);

        statusCode.Should().Be(StatusCodes.Status400BadRequest);
        body!.Success.Should().BeFalse();
        body.Message.Should().Be("Falha de validação.");
        body.Errors.Should().Contain("Nome é obrigatório.");
        body.Errors.Should().Contain("Email é inválido.");
    }

    [Fact]
    public async Task Invoke_ValidationException_DeveDeduplicarErros()
    {
        var erros = new List<ValidationFailure>
        {
            new("Nome", "Mensagem duplicada."),
            new("Email", "Mensagem duplicada.")
        };
        var excecao = new ValidationException(erros);

        var (_, body) = await InvocarMiddleware(excecao);

        body!.Errors.Should().ContainSingle("Mensagem duplicada.");
    }

    [Fact]
    public async Task Invoke_ConflictException_DeveRetornar409()
    {
        var (statusCode, body) = await InvocarMiddleware(new ConflictException("E-mail já cadastrado."));

        statusCode.Should().Be(StatusCodes.Status409Conflict);
        body!.Success.Should().BeFalse();
        body.Message.Should().Be("E-mail já cadastrado.");
        body.Errors.Should().Contain("E-mail já cadastrado.");
    }

    [Fact]
    public async Task Invoke_NotFoundException_DeveRetornar404()
    {
        var (statusCode, body) = await InvocarMiddleware(new NotFoundException("Cliente não encontrado."));

        statusCode.Should().Be(StatusCodes.Status404NotFound);
        body!.Success.Should().BeFalse();
        body.Message.Should().Be("Cliente não encontrado.");
    }

    [Fact]
    public async Task Invoke_DomainValidationException_DeveRetornar400()
    {
        var (statusCode, body) = await InvocarMiddleware(new DomainValidationException("Estado inválido."));

        statusCode.Should().Be(StatusCodes.Status400BadRequest);
        body!.Success.Should().BeFalse();
        body.Message.Should().Be("Estado inválido.");
    }

    [Fact]
    public async Task Invoke_ServiceUnavailableException_DeveRetornar503()
    {
        var (statusCode, body) = await InvocarMiddleware(new ServiceUnavailableException("Banco indisponível."));

        statusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        body!.Success.Should().BeFalse();
        body.Message.Should().Be("Banco indisponível.");
    }

    [Fact]
    public async Task Invoke_ExcecaoGenerica_DeveRetornar500ComMensagemGenerica()
    {
        var (statusCode, body) = await InvocarMiddleware(new Exception("Erro interno qualquer."));

        statusCode.Should().Be(StatusCodes.Status500InternalServerError);
        body!.Success.Should().BeFalse();
        body.Message.Should().Be("Erro interno inesperado.");
        body.Errors.Should().Contain("Ocorreu um erro não mapeado.");
    }

    [Fact]
    public async Task Invoke_ExcecaoGenerica_NaoDeveExporDetalhesInternos()
    {
        var (_, body) = await InvocarMiddleware(new Exception("Detalhe sensível do sistema."));

        body!.Message.Should().NotContain("Detalhe sensível do sistema.");
        body.Errors!.Should().NotContain(e => e.Contains("Detalhe sensível do sistema."));
    }

    [Fact]
    public async Task Invoke_TodosOsTipos_SuccessSempreRetornaFalse()
    {
        var excecoes = new Exception[]
        {
            new ValidationException(new[] { new ValidationFailure("x", "x") }),
            new ConflictException("x"),
            new NotFoundException("x"),
            new DomainValidationException("x"),
            new ServiceUnavailableException("x"),
            new Exception("x")
        };

        foreach (var excecao in excecoes)
        {
            var (_, body) = await InvocarMiddleware(excecao);
            body!.Success.Should().BeFalse($"a exceção {excecao.GetType().Name} deve retornar success=false");
        }
    }
}
