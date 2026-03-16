namespace Models.Domain.Entities;
public class Reminder
{
    public Guid Id { get; set; }
    public DateTime RemindAt { get; set; }
    public bool IsSent { get; set; }

    public Guid TaskId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;
}