namespace ToDo_Project.Models.Patterns.Strategies;

public class MonthlyRepetitionStrategy : IRepetitionStrategy
{
    public DateTime GetNextExecutionDate(DateTime currentDate)
    {
        return currentDate.AddMonths(1);
    }
}