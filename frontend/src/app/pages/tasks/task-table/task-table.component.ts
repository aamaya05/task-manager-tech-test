import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Task } from '../../../models/task.model';
import { PagedResult } from '../../../models/paged-result.model';

@Component({
  selector: 'app-task-table',
  templateUrl: './task-table.component.html',
  standalone: false
})
export class TaskTableComponent {
  @Input() pagedResult: PagedResult<Task> | null = null;
  @Input() loading = false;

  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();
  @Output() editTask = new EventEmitter<Task>();
  @Output() deleteTask = new EventEmitter<string>();

  readonly pageSizeOptions = [5, 10, 25, 50];

  get pages(): number[] {
    if (!this.pagedResult) return [];
    return Array.from({ length: this.pagedResult.totalPages }, (_, i) => i + 1);
  }

  get currentPage(): number {
    return this.pagedResult?.page ?? 1;
  }

  get currentPageSize(): number {
    return this.pagedResult?.pageSize ?? 10;
  }

  onPageClick(page: number): void {
    if (page !== this.currentPage) {
      this.pageChange.emit(page);
    }
  }

  onPageSizeChange(event: Event): void {
    const size = parseInt((event.target as HTMLSelectElement).value, 10);
    this.pageSizeChange.emit(size);
  }

  onEdit(task: Task): void {
    this.editTask.emit(task);
  }

  onDelete(id: string): void {
    this.deleteTask.emit(id);
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'Done': return 'badge bg-success';
      case 'InProgress': return 'badge bg-warning text-dark';
      default: return 'badge bg-secondary';
    }
  }

  formatDate(dateStr: string | null): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }
}
