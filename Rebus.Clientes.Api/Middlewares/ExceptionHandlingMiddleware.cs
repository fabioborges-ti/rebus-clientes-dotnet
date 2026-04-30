using FluentValidation;
using Rebus.Clientes.Api.Models;
using Rebus.Clientes.Domain.Exceptions;

namespace Rebus.Clientes.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Falha de validação. TraceId: {TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ApiResponse
            {
                Success = false,
                Message = "Falha de validação.",
                Errors = ex.Errors.Select(x => x.ErrorMessage).Distinct().ToList()
            });
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "Conflito de negócio. TraceId: {TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Errors = [ex.Message]
            });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Recurso não encontrado. TraceId: {TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Errors = [ex.Message]
            });
        }
        catch (DomainValidationException ex)
        {
            _logger.LogWarning(ex, "Validação de domínio falhou. TraceId: {TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Errors = [ex.Message]
            });
        }
        catch (ServiceUnavailableException ex)
        {
            _logger.LogError(ex, "Dependência externa indisponível. TraceId: {TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Errors = [ex.Message]
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado. TraceId: {TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ApiResponse
            {
                Success = false,
                Message = "Erro interno inesperado.",
                Errors = ["Ocorreu um erro não mapeado."]
            });
        }
    }
}
