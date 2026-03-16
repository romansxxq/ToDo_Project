using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models.Domain.Enums;

namespace ToDo_Project.Models.ViewModels;

public class TaskFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public TypePriority Priority { get; set; }

    public RepetitionType RepetitionType { get; set; }

    public long TelegramChatId { get; set; }

    public IEnumerable<SelectListItem> PriorityOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> RepetitionOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}
