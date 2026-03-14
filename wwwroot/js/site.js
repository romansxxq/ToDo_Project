// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener("DOMContentLoaded", () => {
	const form = document.getElementById("test-todo-form");

	const resultBox = document.getElementById("test-todo-result");
	const dueInput = document.getElementById("todo-due");
	const taskList = document.getElementById("task-list");
	const completedList = document.getElementById("completed-list");
	const completedCount = document.getElementById("analytics-completed");
	const overdueCount = document.getElementById("analytics-overdue");
	const tabs = document.querySelectorAll(".tab");
	const testPanel = document.getElementById("test-panel");
	const globalChatId = document.getElementById("global-chat-id");
	const createChatId = document.getElementById("create-chat-id");
	const chatIdHint = document.getElementById("chat-id-hint");

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
		filter: "today",
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
		const gmt2 = new Date(date.getTime() + 2 * 60 * 60 * 1000);
		return gmt2.toLocaleString(undefined, {
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

	const filterTasks = (task) => {
		const due = new Date(task.dueDate);
		if (state.filter === "today") {
			return !isCompleted(task) && isSameDay(due, new Date());
		}
		if (state.filter === "overdue") {
			return isOverdue(task);
		}
		return !isCompleted(task) && !isOverdue(task);
	};

	const priorityClass = (priority) => {
		switch (priority) {
			case 2:
				return "pill-high";
			case 1:
				return "pill-medium";
			default:
				return "pill-low";
		}
	};

	const renderEmpty = (target, message) => {
		if (!target) {
			return;
		}
		target.innerHTML = `<div class="task-empty">${message}</div>`;
	};

	const renderTasks = () => {
		if (!taskList || !completedList) {
			return;
		}

		const activeTasks = state.tasks.filter(filterTasks);
		const doneTasks = state.tasks.filter(isCompleted);

		if (activeTasks.length === 0) {
			renderEmpty(taskList, "No tasks for this view yet.");
		} else {
				taskList.innerHTML = activeTasks
					.map(
						(task) => `
						<div class="task-card ${isCompleted(task) ? "task-card-completed" : ""}">
							<input class="task-check" type="checkbox" data-id="${task.id}" ${isCompleted(task) ? "checked" : ""} />
							<div>
								<div class="task-title">${task.title}</div>
								<div class="task-meta">${formatDate(task.dueDate)}</div>
							</div>
							<div class="task-meta">${task.description || ""}</div>
							<div class="task-actions">
								<a class="task-link" href="/Tasks/Details/${task.id}">Details</a>
								<a class="task-link" href="/Tasks/Edit/${task.id}">Edit</a>
								<button class="task-delete" type="button" data-id="${task.id}">Delete</button>
							</div>
							<div class="task-pill ${priorityClass(task.priority)}"></div>
						</div>`
					)
					.join("");
		}

		if (doneTasks.length === 0) {
			renderEmpty(completedList, "No completed tasks yet.");
		} else {
			completedList.innerHTML = doneTasks
				.map(
					(task) => `
					<div class="task-card task-card-completed">
						<input class="task-check" type="checkbox" checked disabled />
						<div>
							<div class="task-title">${task.title}</div>
							<div class="task-meta">${formatDate(task.dueDate)}</div>
						</div>
						<div class="task-meta">${task.description || ""}</div>
						<div class="task-actions">
							<a class="task-link" href="/Tasks/Details/${task.id}">Details</a>
							<a class="task-link" href="/Tasks/Edit/${task.id}">Edit</a>
							<button class="task-delete" type="button" data-id="${task.id}">Delete</button>
						</div>
						<div class="task-pill ${priorityClass(task.priority)}"></div>
					</div>`
				)
				.join("");
		}

		if (completedCount) {
			completedCount.textContent = doneTasks.length;
		}
		if (overdueCount) {
			overdueCount.textContent = state.tasks.filter(isOverdue).length;
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
