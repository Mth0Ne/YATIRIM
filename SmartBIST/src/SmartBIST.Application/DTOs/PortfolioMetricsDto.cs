namespace SmartBIST.Application.DTOs;

public class PortfolioMetricsDto
{
    public int PortfolioId { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfitLoss { get; set; }
    public double TotalProfitLossPercentage { get; set; }
    public double DailyReturn { get; set; }
    public double ExpectedAnnualReturn { get; set; }
    public double Volatility { get; set; }
    public double SharpeRatio { get; set; }
    public double SortinoRatio { get; set; }
    public double MaximumDrawdown { get; set; }
    public double PortfolioBeta { get; set; }
    public DateTime CalculationDate { get; set; }
} 