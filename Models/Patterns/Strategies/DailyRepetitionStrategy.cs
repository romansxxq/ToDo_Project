namespace ToDo_Project.Models.Patterns.Strategies;
public class DailyRepetitionStrategy : IRepetitionStrategy
{
    public DateTime GetNextExecutionDate(DateTime currentDate)
    {
        return currentDate.AddDays(1);
    }
}