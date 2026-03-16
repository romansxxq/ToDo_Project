using Models.Domain.Enums;
using Models.Domain.Patterns.Strategies;
namespace Models.Domain.Patterns.Strategies;

public class WeeklyRepetitionStrategy : IRepetitionStrategy
{
    public RepetitionType RepetitionType => RepetitionType.Weekly;
    public DateTime? GetNextExecutionDate(DateTime currentDate) => currentDate.AddDays(7);
}