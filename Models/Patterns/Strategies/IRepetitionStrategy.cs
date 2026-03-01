namespace ToDo_Project.Models.Patterns.Strategies;

public interface IRepetitionStrategy
{
    public DateTime GetNextExecutionDate(DateTime currentDate);
}