namespace DigitalMe.Services.Tools;

/// <summary>
/// Interface for tools that require parameter schemas.
/// Segregates parameter schema functionality from basic tool strategy.
/// This follows ISP - tools that don't need parameters don't have to implement this.
/// </summary>
public interface IParameterizedTool
{
    /// <summary>
    /// Returns the parameter schema for this tool (JSON Schema format).
    /// Only needed for tools that accept complex parameters.
    /// </summary>
    object GetParameterSchema();
}