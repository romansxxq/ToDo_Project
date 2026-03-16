using Models.Domain.Enums;

namespace Models.Domain.Patterns.Strategies;

public interface IRepetitionStrategy
{
    public RepetitionType RepetitionType { get; }
    public DateTime? GetNextExecutionDate(DateTime currentDate);
}