using FluentValidation;
using Rebus.Clientes.Application.Dtos;

namespace Rebus.Clientes.Application.Validators;

public class CreateClienteDtoValidator : AbstractValidator<ClienteWriteDto>
{
    public CreateClienteDtoValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(10).WithMessage("Nome deve possuir pelo menos 10 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email é inválido.");

        RuleFor(x => x.Documento)
            .NotEmpty().WithMessage("Documento é obrigatório.")
            .Length(11).WithMessage("CPF deve conter exatamente 11 dígitos.")
            .Matches(@"^\d{11}$").WithMessage("CPF deve conter apenas números.")
            .Must(CpfValido).WithMessage("CPF inválido.");
    }

    private static bool CpfValido(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf) || cpf.Length != 11)
            return false;

        var n = cpf.Select(c => c - '0').ToArray();

        // Rejeita sequências com todos os dígitos iguais (ex.: 00000000000)
        if (n.Distinct().Count() == 1)
            return false;

        var d1 = 11 - (Enumerable.Range(0, 9).Sum(i => n[i] * (10 - i)) % 11);
        d1 = d1 >= 10 ? 0 : d1;

        var d2 = 11 - ((Enumerable.Range(0, 9).Sum(i => n[i] * (11 - i)) + d1 * 2) % 11);
        d2 = d2 >= 10 ? 0 : d2;

        return n[9] == d1 && n[10] == d2;
    }
}
