using FluentValidation;
using SmartBIST.Application.DTOs;

namespace SmartBIST.Application.Validators;

public class PortfolioDtoValidator : AbstractValidator<PortfolioDto>
{
    public PortfolioDtoValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Portföy adı gereklidir")
            .MaximumLength(100).WithMessage("Portföy adı en fazla 100 karakter olabilir");
            
        RuleFor(p => p.Description)
            .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir");
            
        RuleFor(p => p.InvestmentStrategy)
            .MaximumLength(100).WithMessage("Yatırım stratejisi en fazla 100 karakter olabilir");
            
        RuleFor(p => p.RiskLevel)
            .GreaterThanOrEqualTo(1).WithMessage("Risk seviyesi en az 1 olmalıdır")
            .LessThanOrEqualTo(5).WithMessage("Risk seviyesi en fazla 5 olmalıdır");
            
        RuleFor(p => p.Type)
            .NotEmpty().WithMessage("Portföy tipi gereklidir")
            .Must(t => new[] { "Normal", "Emeklilik", "Agresif", "Pasif" }.Contains(t))
            .WithMessage("Geçerli bir portföy tipi seçiniz (Normal, Emeklilik, Agresif, Pasif)");
            
        RuleFor(p => p.CurrencyCode)
            .NotEmpty().WithMessage("Para birimi gereklidir")
            .Must(c => new[] { "TRY", "USD", "EUR", "GBP" }.Contains(c))
            .WithMessage("Geçerli bir para birimi seçiniz (TRY, USD, EUR, GBP)");
            
        RuleFor(p => p.UserId)
            .NotEmpty().WithMessage("Kullanıcı ID'si gereklidir");
    }
} 