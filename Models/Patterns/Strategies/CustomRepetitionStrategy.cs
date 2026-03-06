using ToDo_Project.Models.Patterns.Strategies;
namespace Models.Patterns.Strategies;

public class CustomRepetitionStrategy : IRepetitionStrategy
{
    public RepetitionType RepetitionType => RepetitionType.Custom;
    private readonly Func<DateTime, DateTime> _getNextExecutionDateFunc;

    public CustomRepetitionStrategy(Func<DateTime, DateTime> getNextExecutionDateFunc)
    {
        _getNextExecutionDateFunc = getNextExecutionDateFunc;
    }

    public DateTime GetNextExecutionDate(DateTime currentDate)
    {
        return _getNextExecutionDateFunc(currentDate);
    }
}