using Models.Domain.Enums;
using Models.Domain.Patterns.Strategies;
namespace Models.Domain.Patterns.Strategies;

public class MonthlyRepetitionStrategy : IRepetitionStrategy
{
    public RepetitionType RepetitionType => RepetitionType.Monthly;
    public DateTime? GetNextExecutionDate(DateTime currentDate) => currentDate.AddMonths(1);
}