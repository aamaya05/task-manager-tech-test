import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { Task, TaskStatus } from '../../../models/task.model';
import { TaskService } from '../../../core/services/task.service';

@Component({
  selector: 'app-task-form',
  templateUrl: './task-form.component.html',
  standalone: false
})
export class TaskFormComponent implements OnInit {
  @Input() task: Task | null = null;
  @Output() saved = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  title = '';
  description = '';
  status: TaskStatus = 'Todo';
  dueDate = '';
  errorMessage = '';
  loading = false;

  readonly statusOptions: TaskStatus[] = ['Todo', 'InProgress', 'Done'];

  get isEditing(): boolean {
    return this.task !== null;
  }

  constructor(private taskService: TaskService) {}

  ngOnInit(): void {
    if (this.task) {
      this.title = this.task.title;
      this.description = this.task.description ?? '';
      this.status = this.task.status;
      this.dueDate = this.task.dueDate
        ? new Date(this.task.dueDate).toISOString().substring(0, 10)
        : '';
    }
  }

  onSubmit(): void {
    if (!this.title.trim()) {
      this.errorMessage = 'Title is required.';
      return;
    }
    this.loading = true;
    this.errorMessage = '';

    const payload = {
      title: this.title.trim(),
      description: this.description.trim() || null,
      status: this.status,
      dueDate: this.dueDate ? new Date(this.dueDate).toISOString() : null
    };

    const request$ = this.isEditing
      ? this.taskService.updateTask(this.task!.id, payload)
      : this.taskService.createTask(payload);

    request$.subscribe({
      next: () => {
        this.loading = false;
        this.saved.emit();
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err?.error?.detail ?? 'Failed to save task.';
      }
    });
  }

  onCancel(): void {
    this.cancelled.emit();
  }
}
