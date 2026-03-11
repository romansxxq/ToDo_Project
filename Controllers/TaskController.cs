using Microsoft.AspNetCore.Mvc;
using Models.Domain.Entities;
using Models.Domain.Enums;
using Services;

namespace ToDo_Project.Controllers;

[Route("tasks")]
public class TaskController : Controller
{
    private readonly TaskService _taskService;

    public TaskController(TaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var tasks = await _taskService.GetAllTasksAsync();
        return Ok(tasks);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
    {
        var task = await _taskService.CreateTaskAsync(
            request.Title,
            request.Description,
            request.DueDate,
            request.Priority,
            request.RepetitionType,
            request.TelegramChatId);

        return CreatedAtAction(nameof(Details), new { id = task.Id }, task);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        await _taskService.CompleteTaskAsync(id);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _taskService.DeleteTaskAsync(id);
        return NoContent();
    }

    public sealed class CreateTaskRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public TypePriority Priority { get; set; }
        public RepetitionType RepetitionType { get; set; }
        public long TelegramChatId { get; set; }
    }
}