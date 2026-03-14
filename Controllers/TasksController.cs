using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models.Domain.Entities;
using Models.Domain.Enums;
using Services;
using ToDo_Project.Models.ViewModels;

namespace ToDo_Project.Controllers;

public class TasksController : Controller
{
    private readonly TaskService _taskService;

    public TasksController(TaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var tasks = await _taskService.GetAllTasksAsync();
        return View(tasks.OrderBy(t => t.DueDate));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task == null)
        {
            return NotFound();
        }

        return View(task);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = BuildFormModel();
        model.DueDate = DateTime.Now.AddHours(2);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(BuildFormModel(model));
        }

        var task = await _taskService.CreateTaskAsync(
            model.Title,
            model.Description,
            model.DueDate,
            model.Priority,
            model.RepetitionType,
            model.TelegramChatId);

        TempData["AlertMessage"] = $"Task '{task.Title}' created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task == null)
        {
            return NotFound();
        }

        var model = BuildFormModel(task);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, TaskFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(BuildFormModel(model));
        }

        var task = await _taskService.GetTaskByIdAsync(id);
        if (task == null)
        {
            return NotFound();
        }

        task.Title = model.Title;
        task.Description = model.Description;
        task.DueDate = model.DueDate;
        task.Priority = model.Priority;
        task.RepetitionType = model.RepetitionType;
        task.TelegramChatId = model.TelegramChatId;

        await _taskService.UpdateTaskAsync(task);

        return RedirectToAction(nameof(Details), new { id });
    }

    private static TaskFormViewModel BuildFormModel(TaskItem task)
    {
        var model = new TaskFormViewModel
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            Priority = task.Priority,
            RepetitionType = task.RepetitionType,
            TelegramChatId = task.TelegramChatId
        };

        return BuildFormModel(model);
    }

    private static TaskFormViewModel BuildFormModel(TaskFormViewModel? model = null)
    {
        var viewModel = model ?? new TaskFormViewModel();

        viewModel.PriorityOptions = Enum.GetValues<TypePriority>()
            .Select(value => new SelectListItem(value.ToString(), ((int)value).ToString()));

        viewModel.RepetitionOptions = Enum.GetValues<RepetitionType>()
            .Select(value => new SelectListItem(value.ToString(), ((int)value).ToString()));

        return viewModel;
    }
}
