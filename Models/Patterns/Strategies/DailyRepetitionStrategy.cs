using ToDo_Project.Models.Domain.Enums;
using ToDo_Project.Models.Patterns.Strategies;
namespace ToDo_Project.Models.Patterns.Strategies;

public class DailyRepetitionStrategy : IRepetitionStrategy
{
        public RepetitionType RepetitionType => RepetitionType.Daily;
        public DateTime GetNextExecutionDate(DateTime currentDate) => currentDate.AddDays(1);
}