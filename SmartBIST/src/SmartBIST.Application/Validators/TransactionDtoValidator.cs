using FluentValidation;
using SmartBIST.Application.DTOs;
using System;
using System.Linq;
using SmartBIST.Core.Entities;

namespace SmartBIST.Application.Validators;

public class TransactionDtoValidator : AbstractValidator<TransactionDto>
{
    public TransactionDtoValidator()
    {
        RuleFor(t => t.PortfolioId)
            .GreaterThan(0).WithMessage("Lütfen geçerli bir portföy seçiniz");
            
        RuleFor(t => t.StockId)
            .GreaterThan(0).WithMessage("Lütfen geçerli bir hisse senedi seçiniz");
            
        RuleFor(t => t.Type)
            .NotEmpty().WithMessage("İşlem tipi gereklidir")
            .IsInEnum().WithMessage("Geçerli bir işlem tipi seçiniz");
            
        RuleFor(t => t.Quantity)
            .GreaterThan(0).WithMessage("Miktar sıfırdan büyük olmalıdır");
            
        RuleFor(t => t.Price)
            .GreaterThan(0).WithMessage("Fiyat sıfırdan büyük olmalıdır");
            
        RuleFor(t => t.TransactionDate)
            .NotNull().WithMessage("İşlem tarihi gereklidir")
            .LessThanOrEqualTo(DateTime.Now).WithMessage("İşlem tarihi gelecek bir tarih olamaz");
            
        RuleFor(t => t.Commission)
            .GreaterThanOrEqualTo(0).WithMessage("Komisyon sıfır veya daha büyük olmalıdır");
            
        // Satış işlemi için bakiye kontrolü (bu kontrolü servis katmanında yapalım)
        RuleFor(t => t)
            .Custom((transaction, context) => {
                // Stok miktarı kontrolü servis katmanında yapılacak
                if (transaction.Type == TransactionType.Sell && transaction.Quantity <= 0)
                {
                    context.AddFailure("Quantity", "Satış işlemi için miktar sıfırdan büyük olmalıdır");
                }
            });
    }
} 