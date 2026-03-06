using ToDo_Project.Models.Domain.Enums;

namespace ToDo_Project.Models.Patterns.Strategies;

public interface IRepetitionStrategy
{
    RepetitionType RepetitionType { get; }
    public DateTime GetNextExecutionDate(DateTime currentDate);
}