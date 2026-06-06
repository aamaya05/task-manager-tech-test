import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Task } from '../../models/task.model';
import { PagedResult } from '../../models/paged-result.model';
import { TaskService } from '../../core/services/task.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-tasks',
  templateUrl: './tasks.component.html',
  standalone: false
})
export class TasksComponent implements OnInit {
  pagedResult: PagedResult<Task> | null = null;
  page = 1;
  pageSize = 10;
  loading = false;
  errorMessage = '';

  showForm = false;
  editingTask: Task | null = null;

  constructor(
    private taskService: TaskService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.loading = true;
    this.errorMessage = '';
    this.taskService.getPagedTasks(this.page, this.pageSize).subscribe({
      next: result => {
        this.pagedResult = result;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Failed to load tasks.';
        this.loading = false;
      }
    });
  }

  onPageChange(newPage: number): void {
    this.page = newPage;
    this.loadTasks();
  }

  onPageSizeChange(newSize: number): void {
    this.pageSize = newSize;
    this.page = 1;
    this.loadTasks();
  }

  openCreateForm(): void {
    this.editingTask = null;
    this.showForm = true;
  }

  openEditForm(task: Task): void {
    this.editingTask = task;
    this.showForm = true;
  }

  onFormSaved(): void {
    this.showForm = false;
    this.editingTask = null;
    this.loadTasks();
  }

  onFormCancelled(): void {
    this.showForm = false;
    this.editingTask = null;
  }

  onDeleteTask(id: string): void {
    if (!confirm('Delete this task?')) return;
    this.taskService.deleteTask(id).subscribe({
      next: () => this.loadTasks(),
      error: () => { this.errorMessage = 'Failed to delete task.'; }
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  get username(): string {
    return this.authService.getUsername() ?? 'User';
  }
}
