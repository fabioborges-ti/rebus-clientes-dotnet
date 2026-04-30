using FluentAssertions;
using FluentValidation.TestHelper;
using Rebus.Clientes.Application.Dtos;
using Rebus.Clientes.Application.Validators;

namespace Rebus.Clientes.UnitTests.Application.Validators;

public class CreateClienteDtoValidatorTests
{
    private readonly CreateClienteDtoValidator _validator = new();

    private const string CpfValido = "52998224725";  // 529.982.247-25
    private const string CpfValido2 = "11144477735"; // 111.444.777-35
    private const string CpfValido3 = "12345678909"; // 123.456.789-09

    private ClienteWriteDto DtoValido() => new()
    {
        Nome = "João da Silva Santos",
        Email = "joao@email.com",
        Documento = CpfValido
    };

    // ── Nome ──────────────────────────────────────────────────────────────

    [Fact]
    public void Nome_Vazio_DeveFalhar()
    {
        var dto = DtoValido();
        dto.Nome = "";

        _validator.TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.Nome)
            .WithErrorMessage("Nome é obrigatório.");
    }

    [Theory]
    [InlineData("a")]
    [InlineData("Curto")]
    [InlineData("123456789")] // 9 chars
    public void Nome_MenorQue10Caracteres_DeveFalhar(string nome)
    {
        var dto = DtoValido();
        dto.Nome = nome;

        _validator.TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public void Nome_ComExatamente10Caracteres_DevePassar()
    {
        var dto = DtoValido();
        dto.Nome = "1234567890";

        _validator.TestValidate(dto)
            .ShouldNotHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public void Nome_Valido_DevePassar()
    {
        _validator.TestValidate(DtoValido())
            .ShouldNotHaveValidationErrorFor(x => x.Nome);
    }

    // ── Email ─────────────────────────────────────────────────────────────

    [Fact]
    public void Email_Vazio_DeveFalhar()
    {
        var dto = DtoValido();
        dto.Email = "";

        _validator.TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email é obrigatório.");
    }

    [Theory]
    [InlineData("sem-arroba")]
    [InlineData("@semdominio.com")]
    [InlineData("invalido")]
    [InlineData("a@")]
    public void Email_Invalido_DeveFalhar(string email)
    {
        var dto = DtoValido();
        dto.Email = email;

        _validator.TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("joao@gmail.com")]
    [InlineData("teste.nome+tag@empresa.com.br")]
    [InlineData("A@B.COM")]
    public void Email_Valido_DevePassar(string email)
    {
        var dto = DtoValido();
        dto.Email = email;

        _validator.TestValidate(dto)
            .ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    // ── CPF / Documento ───────────────────────────────────────────────────

    [Fact]
    public void Documento_Vazio_DeveFalhar()
    {
        var dto = DtoValido();
        dto.Documento = "";

        _validator.TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.Documento)
            .WithErrorMessage("Documento é obrigatório.");
    }

    [Theory]
    [InlineData("1234")]           // menos de 11
    [InlineData("123456789012")]   // mais de 11
    public void Documento_TamanhoErrado_DeveFalhar(string doc)
    {
        var dto = DtoValido();
        dto.Documento = doc;

        _validator.TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.Documento);
    }

    [Theory]
    [InlineData("1234567890a")]     // letra no meio
    [InlineData("123.456.789-09")] // com pontuação
    [InlineData("123 456 789 0")]  // com espaços
    public void Documento_ComCaracteresNaoNumericos_DeveFalhar(string doc)
    {
        var dto = DtoValido();
        dto.Documento = doc;

        _validator.TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.Documento);
    }

    [Theory]
    [InlineData("00000000000")]
    [InlineData("11111111111")]
    [InlineData("22222222222")]
    [InlineData("33333333333")]
    [InlineData("44444444444")]
    [InlineData("55555555555")]
    [InlineData("66666666666")]
    [InlineData("77777777777")]
    [InlineData("88888888888")]
    [InlineData("99999999999")]
    public void Documento_SequenciaRepetida_DeveFalhar(string cpf)
    {
        var dto = DtoValido();
        dto.Documento = cpf;

        _validator.TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.Documento)
            .WithErrorMessage("CPF inválido.");
    }

    [Theory]
    [InlineData("52998224799")] // dígitos verificadores errados
    [InlineData("12345678901")] // último dígito errado
    [InlineData("11144477730")] // dígito d1 errado
    public void Documento_DigitosVerificadoresInvalidos_DeveFalhar(string cpf)
    {
        var dto = DtoValido();
        dto.Documento = cpf;

        _validator.TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.Documento)
            .WithErrorMessage("CPF inválido.");
    }

    [Theory]
    [InlineData(CpfValido)]
    [InlineData(CpfValido2)]
    [InlineData(CpfValido3)]
    public void Documento_CpfValido_DevePassar(string cpf)
    {
        var dto = DtoValido();
        dto.Documento = cpf;

        _validator.TestValidate(dto)
            .ShouldNotHaveValidationErrorFor(x => x.Documento);
    }

    // ── DTO completo ──────────────────────────────────────────────────────

    [Fact]
    public void DtoCompleto_Valido_NaoDeveTerErros()
    {
        var result = _validator.TestValidate(DtoValido());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DtoCompleto_TodosCamposInvalidos_DeveRetornarMultiplosErros()
    {
        var dto = new ClienteWriteDto { Nome = "x", Email = "invalido", Documento = "00000000000" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Nome);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Documento);
    }
}
