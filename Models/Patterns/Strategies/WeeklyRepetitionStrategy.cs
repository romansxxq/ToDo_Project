namespace ToDo_Project.Models.Patterns.Strategies;

public class WeeklyRepetitionStrategy : IRepetitionStrategy
{
    public DateTime GetNextExecutionDate(DateTime currentDate)
    {
        return currentDate.AddDays(7);
    }
}