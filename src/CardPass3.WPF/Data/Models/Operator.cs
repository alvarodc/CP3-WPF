namespace CardPass3.WPF.Data.Models;

public class Operator
{
    public int IdOperator { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string OperatorDescription { get; set; } = string.Empty;

    public List<string> FunctionNames { get; set; } = new();
}
