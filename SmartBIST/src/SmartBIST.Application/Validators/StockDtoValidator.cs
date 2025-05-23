using FluentValidation;
using SmartBIST.Application.DTOs;

namespace SmartBIST.Application.Validators;

public class StockDtoValidator : AbstractValidator<StockDto>
{
    public StockDtoValidator()
    {
        RuleFor(s => s.Symbol)
            .NotEmpty().WithMessage("Hisse sembolü gereklidir")
            .MaximumLength(10).WithMessage("Hisse sembolü en fazla 10 karakter olabilir")
            .Matches("^[A-Z0-9.]+$").WithMessage("Hisse sembolü sadece büyük harf, rakam ve nokta içerebilir");
            
        RuleFor(s => s.Name)
            .NotEmpty().WithMessage("Hisse adı gereklidir")
            .MaximumLength(100).WithMessage("Hisse adı en fazla 100 karakter olabilir");
            
        RuleFor(s => s.CurrentPrice)
            .GreaterThan(0).WithMessage("Güncel fiyat sıfırdan büyük olmalıdır");
    }
} 