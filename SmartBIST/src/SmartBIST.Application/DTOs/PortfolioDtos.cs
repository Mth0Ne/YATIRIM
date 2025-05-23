using System;

namespace SmartBIST.Application.DTOs;

public class CreatePortfolioDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdatePortfolioDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
} 