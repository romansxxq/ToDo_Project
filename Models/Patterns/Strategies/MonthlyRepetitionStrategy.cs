using ToDo_Project.Models.Domain.Enums;
using ToDo_Project.Models.Patterns.Strategies;
namespace ToDo_Project.Models.Patterns.Strategies;

public class MonthlyRepetitionStrategy : IRepetitionStrategy
{
    public RepetitionType RepetitionType => RepetitionType.Monthly;
    public DateTime GetNextExecutionDate(DateTime currentDate) => currentDate.AddMonths(1);
}