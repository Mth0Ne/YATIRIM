using System.ComponentModel.DataAnnotations;

namespace SmartBIST.WebUI.Models;

public class UserSettingsViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
    [Display(Name = "Ad Soyad")]
    [StringLength(50, ErrorMessage = "Adınız en fazla 50 karakter olabilir")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "E-posta adresi zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;
    
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    [Display(Name = "Telefon")]
    public string Phone { get; set; } = string.Empty;
    
    [Display(Name = "E-posta Onaylandı")]
    public bool EmailConfirmed { get; set; }
    
    [Display(Name = "Telefon Onaylandı")]
    public bool PhoneConfirmed { get; set; }
    
    [Display(Name = "İki Faktörlü Doğrulama")]
    public bool TwoFactorEnabled { get; set; }
} 