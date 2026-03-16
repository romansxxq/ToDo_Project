using Models.Domain.Enums;
using Models.Domain.Patterns.Strategies;
namespace Models.Domain.Patterns.Strategies;

public class DailyRepetitionStrategy : IRepetitionStrategy
{
        public RepetitionType RepetitionType => RepetitionType.Daily;
        public DateTime? GetNextExecutionDate(DateTime currentDate) => currentDate.AddDays(1);
}