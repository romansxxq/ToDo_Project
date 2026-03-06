using ToDo_Project.Models.Domain.Enums;
using ToDo_Project.Models.Patterns.Strategies;
namespace ToDo_Project.Models.Patterns.Strategies;

public class WeeklyRepetitionStrategy : IRepetitionStrategy
{
    public RepetitionType RepetitionType => RepetitionType.Weekly;
    public DateTime GetNextExecutionDate(DateTime currentDate) => currentDate.AddDays(7);
}