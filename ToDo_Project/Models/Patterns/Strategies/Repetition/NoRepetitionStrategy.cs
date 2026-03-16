using Models.Domain.Enums;
namespace Models.Domain.Patterns.Strategies;
public class NoRepetitionStrategy : IRepetitionStrategy
{
    public RepetitionType RepetitionType => RepetitionType.None;
    public DateTime? GetNextExecutionDate(DateTime currentDate) => null;
}