using Models.Domain.Enums;
namespace ToDo_Project.Models.Domain.Entities;
public abstract class Task
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public DateTime DueDate { get; set; }
    public TypePriority Priority { get; set; }
    public TaskStatus Status { get; set; }
}