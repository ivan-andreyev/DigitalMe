namespace DigitalMe.Models.Database;

/// <summary>
/// Helper class for PostgreSQL compatibility with EF Core SqlQuery<T>.
///
/// Problem: EF Core automatically generates PascalCase alias 't.Value' for SqlQuery<int>
/// which conflicts with PostgreSQL case-sensitivity (expects 't.value')
///
/// Solution: Use lowercase property name 'value' to match PostgreSQL expectations
/// </summary>
public class QueryResult
{
    /// <summary>
    /// Lowercase property name for PostgreSQL compatibility.
    /// Maps to SQL alias 'as value' instead of EF Core's default 't.Value'
    /// </summary>
    public int value { get; set; }
}