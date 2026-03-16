// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener("DOMContentLoaded", () => {
	const form = document.getElementById("test-todo-form");

	const resultBox = document.getElementById("test-todo-result");
	const dueInput = document.getElementById("todo-due");
	const taskList = document.getElementById("task-list");
	const completedList = document.getElementById("completed-list");
	const totalCount = document.getElementById("analytics-total");
	const completedCount = document.getElementById("analytics-completed");
	const pendingCount = document.getElementById("analytics-pending");
	const overdueCount = document.getElementById("analytics-overdue");
	const taskCount = document.getElementById("task-count");
	const tabs = document.querySelectorAll(".tab");
	const testPanel = document.getElementById("test-panel");
	const globalChatId = document.getElementById("global-chat-id");
	const createChatId = document.getElementById("create-chat-id");
	const chatIdHint = document.getElementById("chat-id-hint");
	const priorityFilter = document.getElementById("priority-filter");
	const searchInput = document.getElementById("task-search");

	if (dueInput && !dueInput.value) {
		const now = new Date();
		now.setHours(now.getHours() + 2);
		dueInput.value = now.toISOString().slice(0, 16);
	}

	if (globalChatId) {
		const savedChatId = window.localStorage.getItem("todoChatId");
		if (savedChatId) {
			globalChatId.value = savedChatId;
		}
		globalChatId.addEventListener("input", () => {
			if (globalChatId.value) {
				window.localStorage.setItem("todoChatId", globalChatId.value);
			} else {
				window.localStorage.removeItem("todoChatId");
			}
		});
	}

	if (createChatId) {
		const savedChatId = window.localStorage.getItem("todoChatId");
		if (savedChatId) {
			createChatId.value = savedChatId;
			if (chatIdHint) {
				chatIdHint.textContent = "Using the saved Telegram Chat ID.";
			}
		} else if (chatIdHint) {
			chatIdHint.textContent = "No Telegram Chat ID saved yet. Set it on the Tasks page.";
		}
	}

	const state = {
		filter: "all",
		priority: "all",
		search: "",
		tasks: []
	};

	const showAlert = (message) => {
		if (!message) {
			return;
		}
		window.alert(message);
	};

	const formatDate = (value) => {
		if (!value) {
			return "";
		}
		const date = new Date(value);
		return date.toLocaleString(undefined, {
			month: "short",
			day: "numeric",
			year: "numeric",
			hour: "2-digit",
			minute: "2-digit"
		});
	};

	const isSameDay = (a, b) =>
		a.getFullYear() === b.getFullYear() &&
		a.getMonth() === b.getMonth() &&
		a.getDate() === b.getDate();

	const getStatusValue = (status) => {
		if (typeof status === "number") {
			return status;
		}
		if (typeof status === "string") {
			switch (status.toLowerCase()) {
				case "completed":
					return 1;
				case "overdue":
					return 2;
				default:
					return 0;
			}
		}
		return 0;
	};

	const getStatusLabel = (status) => {
		if (typeof status === "string") {
			return status;
		}
		switch (getStatusValue(status)) {
			case 1:
				return "Completed";
			case 2:
				return "Overdue";
			default:
				return "Pending";
		}
	};

	const getPriorityLabel = (priority) => {
		switch (priority) {
			case 2:
				return "High";
			case 1:
				return "Medium";
			default:
				return "Low";
		}
	};

	const isCompleted = (task) => getStatusValue(task.status) === 1;

	const isOverdue = (task) => {
		if (isCompleted(task)) {
			return false;
		}
		if (getStatusValue(task.status) === 2) {
			return true;
		}
		const due = new Date(task.dueDate);
		return due < new Date();
	};

	const matchesFilters = (task) => {
		if (state.priority !== "all") {
			const priorityLabel = getPriorityLabel(task.priority).toLowerCase();
			if (priorityLabel !== state.priority) {
				return false;
			}
		}
		if (state.search) {
			const haystack = `${task.title || ""} ${task.description || ""}`.toLowerCase();
			if (!haystack.includes(state.search)) {
				return false;
			}
		}
		return true;
	};

	const priorityClass = (priority) => {
		switch (priority) {
			case 2:
				return "bg-red-50 text-red-700 border-red-200";
			case 1:
				return "bg-amber-50 text-amber-700 border-amber-200";
			default:
				return "bg-emerald-50 text-emerald-700 border-emerald-200";
		}
	};

	const statusClass = (status) => {
		switch (getStatusLabel(status)) {
			case "Completed":
				return "bg-emerald-50 text-emerald-700 border-emerald-200";
			case "Overdue":
				return "bg-red-50 text-red-700 border-red-200";
			default:
				return "bg-amber-50 text-amber-700 border-amber-200";
		}
	};

	const renderEmpty = (target, message) => {
		if (!target) {
			return;
		}
		target.innerHTML = `
			<tr>
				<td colspan="5" class="px-6 py-10 text-center text-sm text-muted-foreground">
					${message}
				</td>
			</tr>`;
	};

	const renderTasks = () => {
		if (!taskList || !completedList) {
			return;
		}

		const activeTasks = state.tasks.filter((task) => {
			if (isCompleted(task)) {
				return false;
			}
			if (state.filter === "completed") {
				return false;
			}
			if (state.filter === "overdue" && !isOverdue(task)) {
				return false;
			}
			if (state.filter === "pending" && isOverdue(task)) {
				return false;
			}
			return matchesFilters(task);
		});
		const doneTasks = state.tasks.filter((task) => isCompleted(task) && matchesFilters(task));

		if (activeTasks.length === 0) {
			renderEmpty(taskList, "No tasks for this view yet.");
		} else {
				taskList.innerHTML = activeTasks
					.map(
						(task) => `
						<tr class="hover:bg-background transition-colors">
							<td class="px-6 py-4">
								<div class="flex items-center gap-3">
									<input class="task-check" type="checkbox" data-id="${task.id}" ${isCompleted(task) ? "checked" : ""} ${isCompleted(task) ? "disabled" : ""} />
									<div class="min-w-0">
										<div class="text-sm font-semibold text-card-foreground truncate">${task.title}</div>
										<div class="text-xs text-muted-foreground truncate">${task.description || ""}</div>
									</div>
								</div>
							</td>
							<td class="px-6 py-4 text-sm tabular-nums text-muted-foreground hidden md:table-cell">${formatDate(task.dueDate)}</td>
							<td class="px-6 py-4 hidden sm:table-cell">
								<span class="px-2 py-0.5 rounded text-xs font-medium border ${priorityClass(task.priority)}">${getPriorityLabel(task.priority)}</span>
							</td>
							<td class="px-6 py-4 hidden sm:table-cell">
								<span class="px-2 py-0.5 rounded text-xs font-medium border ${statusClass(task.status)}">${getStatusLabel(task.status)}</span>
							</td>
							<td class="px-6 py-4 text-right">
								<div class="flex items-center justify-end gap-2 text-xs">
									<a class="text-primary hover:underline" href="/Tasks/Details/${task.id}">View</a>
									<a class="text-primary hover:underline" href="/Tasks/Edit/${task.id}">Edit</a>
									<button class="task-delete text-destructive" type="button" data-id="${task.id}">Delete</button>
								</div>
							</td>
						</tr>`
					)
					.join("");
		}

		if (doneTasks.length === 0) {
			renderEmpty(completedList, "No completed tasks yet.");
		} else {
			completedList.innerHTML = doneTasks
				.map(
					(task) => `
					<tr class="hover:bg-background transition-colors">
						<td class="px-6 py-4">
							<div class="flex items-center gap-3">
								<input class="task-check" type="checkbox" checked disabled />
								<div class="min-w-0">
									<div class="text-sm font-semibold text-card-foreground line-through truncate">${task.title}</div>
									<div class="text-xs text-muted-foreground truncate">${task.description || ""}</div>
								</div>
							</div>
						</td>
						<td class="px-6 py-4 text-sm tabular-nums text-muted-foreground hidden md:table-cell">${formatDate(task.dueDate)}</td>
						<td class="px-6 py-4 hidden sm:table-cell">
							<span class="px-2 py-0.5 rounded text-xs font-medium border ${priorityClass(task.priority)}">${getPriorityLabel(task.priority)}</span>
						</td>
						<td class="px-6 py-4 hidden sm:table-cell">
							<span class="px-2 py-0.5 rounded text-xs font-medium border ${statusClass("Completed")}">Completed</span>
						</td>
						<td class="px-6 py-4 text-right">
							<div class="flex items-center justify-end gap-2 text-xs">
								<a class="text-primary hover:underline" href="/Tasks/Details/${task.id}">View</a>
								<a class="text-primary hover:underline" href="/Tasks/Edit/${task.id}">Edit</a>
								<button class="task-delete text-destructive" type="button" data-id="${task.id}">Delete</button>
							</div>
						</td>
					</tr>`
				)
				.join("");
		}

		if (taskCount) {
			taskCount.textContent = activeTasks.length;
		}
		updateAnalyticsFromTasks();
	};

	const updateAnalyticsFromTasks = () => {
		const completed = state.tasks.filter(isCompleted).length;
		const overdue = state.tasks.filter(isOverdue).length;
		const pending = state.tasks.length - completed - overdue;
		if (totalCount) {
			totalCount.textContent = state.tasks.length;
		}
		if (completedCount) {
			completedCount.textContent = completed;
		}
		if (pendingCount) {
			pendingCount.textContent = pending;
		}
		if (overdueCount) {
			overdueCount.textContent = overdue;
		}
	};

	const loadTasks = async () => {
		try {
			const response = await fetch("/api/tasks");
			if (!response.ok) {
				throw new Error(`Failed to load tasks (${response.status}).`);
			}
			state.tasks = await response.json();
			renderTasks();
		} catch (error) {
			if (taskList) {
				renderEmpty(taskList, "Failed to load tasks.");
			}
		}
	};

	const handleComplete = async (event) => {
		const target = event.target;
		if (!target.classList.contains("task-check") || !target.dataset.id) {
			return;
		}

		const task = state.tasks.find((item) => item.id === target.dataset.id);
		if (!task || isCompleted(task)) {
			return;
		}

		try {
			const response = await fetch(`/api/tasks/${target.dataset.id}/complete`, {
				method: "POST"
			});
			if (!response.ok) {
				throw new Error("Complete failed.");
			}
			await loadTasks();
			showAlert("Task marked as completed.");
		} catch (error) {
			target.checked = false;
		}
	};

	const handleDelete = async (event) => {
		const target = event.target;
		if (!target.classList.contains("task-delete") || !target.dataset.id) {
			return;
		}

		const confirmed = window.confirm("Delete this task?");
		if (!confirmed) {
			return;
		}

		try {
			const response = await fetch(`/api/tasks/${target.dataset.id}`, {
				method: "DELETE"
			});
			if (!response.ok) {
				throw new Error("Delete failed.");
			}
			await loadTasks();
		} catch (error) {
			window.alert("Failed to delete task.");
		}
	};

	if (tabs.length > 0) {
		tabs.forEach((tab) => {
			tab.addEventListener("click", () => {
				tabs.forEach((item) => item.classList.remove("active"));
				tab.classList.add("active");
				state.filter = tab.dataset.filter;
				renderTasks();
			});
		});
	}

	if (priorityFilter) {
		priorityFilter.addEventListener("change", () => {
			state.priority = priorityFilter.value;
			renderTasks();
		});
	}

	if (searchInput) {
		searchInput.addEventListener("input", () => {
			state.search = searchInput.value.trim().toLowerCase();
			renderTasks();
		});
	}

	if (taskList) {
		taskList.addEventListener("change", handleComplete);
		taskList.addEventListener("click", handleDelete);
	}

	if (completedList) {
		completedList.addEventListener("click", handleDelete);
	}

	if (testPanel) {
		testPanel.classList.add("hidden");
		testPanel.setAttribute("aria-hidden", "true");
	}

	if (form) {
		form.addEventListener("submit", async (event) => {
			event.preventDefault();

			const chatIdValue = document.getElementById("todo-chat").value.trim();
			const fallbackChatId = globalChatId ? globalChatId.value.trim() : "";
			const payload = {
				title: document.getElementById("todo-title").value.trim(),
				description: document.getElementById("todo-description").value.trim(),
				dueDate: new Date(document.getElementById("todo-due").value).toISOString(),
				priority: Number(document.getElementById("todo-priority").value),
				repetitionType: Number(document.getElementById("todo-repetition").value),
				telegramChatId: Number(chatIdValue || fallbackChatId)
			};

			if (resultBox) {
				resultBox.textContent = "Sending...";
			}

			if (!payload.telegramChatId) {
				if (resultBox) {
					resultBox.textContent = "Telegram Chat ID is required.";
				}
				return;
			}

			try {
				const response = await fetch("/api/tasks", {
					method: "POST",
					headers: { "Content-Type": "application/json" },
					body: JSON.stringify(payload)
				});

				const text = await response.text();
				if (!response.ok) {
					if (resultBox) {
						resultBox.textContent = `Error ${response.status}: ${text}`;
					}
					return;
				}

				if (resultBox) {
					resultBox.textContent = text || "Created.";
				}
				await loadTasks();
				showAlert("Task created successfully.");
			} catch (error) {
				if (resultBox) {
					resultBox.textContent = `Request failed: ${error}`;
				}
			}
		});
	}

	loadTasks();
});
