import { useState, useEffect } from 'react';
import { Card } from '@/components/Card/Card';
import { Button } from '@/components/Button/Button';
import { Loading, ErrorState } from '@/components/Loading/Loading';
import { ConfirmDialog } from '@/components/ConfirmDialog/ConfirmDialog';
import { expenseApi } from '@/services/expense.api';
import { formatCurrency, formatDate } from '@/utils/helpers';
import type { RecurringExpense } from '@/types';
import styles from './RecurringExpensesList.module.css';

interface RecurringExpensesListProps {
  expenseBookId?: string;
  onPaymentSuccess?: () => void;
  startDate?: string;
  endDate?: string;
  onShowSuccess?: (message: string) => void;
  onShowError?: (message: string) => void;
}

export const RecurringExpensesList = ({ 
  expenseBookId,
  startDate = '', 
  endDate = '',
  onShowSuccess,
  onShowError,
}: RecurringExpensesListProps) => {
  const [recurringExpenses, setRecurringExpenses] = useState<RecurringExpense[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const itemsPerPage = 30;

  // Fetch recurring expenses
  const fetchRecurringExpenses = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await expenseApi.getRecurringExpenses({
        expenseBookId: expenseBookId || undefined,
        startDate: startDate || undefined,
        endDate: endDate || undefined,
      });

      if (response.success && response.data) {
        setRecurringExpenses(response.data);
        setCurrentPage(1);
      } else {
        setError(response.error || 'Failed to fetch recurring expenses');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch recurring expenses');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRecurringExpenses();
  }, [startDate, endDate, expenseBookId]);

  const handleDeleteRecurring = async (id: string) => {
    setIsDeleting(true);
    try {
      const response = await expenseApi.deleteRecurringExpense(id);
      if (response.success) {
        onShowSuccess?.('Recurring expense deleted successfully');
        await fetchRecurringExpenses();
      } else {
        onShowError?.(response.error || 'Failed to delete recurring expense');
      }
    } catch (err) {
      onShowError?.(err instanceof Error ? err.message : 'Failed to delete recurring expense');
    } finally {
      setIsDeleting(false);
      setDeleteConfirmId(null);
    }
  };

  const getCategoryIcon = (category: string) => {
    const icons: Record<string, string> = {
      food: '🍔',
      transport: '🚗',
      shopping: '🛍️',
      bills: '📱',
      entertainment: '🎬',
      subscriptions: '🎯',
      healthcare: '🏥',
      education: '📚',
    };
    return icons[category.toLowerCase()] || '💰';
  };

  const getFrequencyDisplay = (frequency: string) => {
    const map: Record<string, string> = {
      daily: 'Daily',
      weekly: 'Weekly',
      monthly: 'Monthly',
      yearly: 'Yearly',
    };
    return map[frequency] || frequency;
  };

  const getDueDateStatus = (nextOccurrence: string) => {
    const nextDate = new Date(nextOccurrence);
    const today = new Date();
    const daysUntilDue = Math.floor((nextDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

    if (daysUntilDue < 0) {
      return { label: 'Overdue', className: 'overdue', days: Math.abs(daysUntilDue) };
    } else if (daysUntilDue <= 3) {
      return { label: 'Due Soon', className: 'due-soon', days: daysUntilDue };
    } else {
      return { label: 'Upcoming', className: 'upcoming', days: daysUntilDue };
    }
  };

  // Pagination
  const startIndex = (currentPage - 1) * itemsPerPage;
  const endIndex = startIndex + itemsPerPage;
  const paginatedExpenses = recurringExpenses.slice(startIndex, endIndex);
  const totalPages = Math.ceil(recurringExpenses.length / itemsPerPage);

  if (loading) return <Loading />;
  if (error) return <ErrorState title="Failed to Load" description={error} onRetry={fetchRecurringExpenses} />;

  return (
    <>
      <div className={styles.container}>
        {/* List */}
        {paginatedExpenses.length === 0 ? (
          <Card>
            <div className={styles.emptyState}>
              <div className={styles.emptyIcon}>📅</div>
              <div className={styles.emptyTitle}>No recurring expenses</div>
              <div className={styles.emptyDescription}>
                {startDate || endDate 
                  ? 'No recurring expenses found in the selected date range.'
                  : 'No recurring expenses found. Add one to track your regular payments!'}
              </div>
            </div>
          </Card>
        ) : (
          <>
            <div className={styles.listContainer}>
              {paginatedExpenses.map((recurring) => {
                const status = getDueDateStatus(recurring.nextOccurrence);
                return (
                  <Card key={recurring.id} className={styles.recurringCard}>
                    <div className={styles.cardContent}>
                      <div className={styles.iconSection}>
                        <div className={styles.icon}>{getCategoryIcon(recurring.category)}</div>
                      </div>

                      <div className={styles.detailsSection}>
                        <div className={styles.header}>
                          <div className={styles.description}>{recurring.description}</div>
                          <span className={`${styles.badge} ${styles[status.className]}`}>
                            {status.label}
                          </span>
                        </div>

                        <div className={styles.meta}>
                          <span className={styles.category}>{recurring.category}</span>
                          <span className={styles.separator}>•</span>
                          <span className={styles.frequency}>{getFrequencyDisplay(recurring.frequency)}</span>
                          <span className={styles.separator}>•</span>
                          <span className={styles.dueDate}>
                            Next: {formatDate(recurring.nextOccurrence, 'short')}
                          </span>
                        </div>
                      </div>

                      <div className={styles.actionSection}>
                        <div className={styles.amount}>{formatCurrency(recurring.amount)}</div>
                        <div className={styles.payNote}>
                          Pay from Dashboard
                        </div>
                        <div className={styles.actionButtons}>
                          <Button
                            variant="ghost"
                            size="small"
                            onClick={() => onShowError?.('Edit functionality coming soon')}
                            title="Edit recurring expense"
                          >
                            <i className="fa-solid fa-edit"></i>
                          </Button>
                          <Button
                            variant="ghost"
                            size="small"
                            onClick={() => setDeleteConfirmId(recurring.id)}
                            title="Delete recurring expense"
                          >
                            <i className="fa-solid fa-trash"></i>
                          </Button>
                        </div>
                      </div>
                    </div>
                  </Card>
                );
              })}
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className={styles.pagination}>
                <Button
                  variant="ghost"
                  onClick={() => setCurrentPage(Math.max(1, currentPage - 1))}
                  disabled={currentPage === 1}
                >
                  <i className="fa-solid fa-chevron-left"></i>
                </Button>
                <span className={styles.pageInfo}>
                  Page {currentPage} of {totalPages}
                </span>
                <Button
                  variant="ghost"
                  onClick={() => setCurrentPage(Math.min(totalPages, currentPage + 1))}
                  disabled={currentPage === totalPages}
                >
                  <i className="fa-solid fa-chevron-right"></i>
                </Button>
              </div>
            )}
          </>
        )}
      </div>

      {/* Delete Confirmation Dialog */}
      {deleteConfirmId && (
        <ConfirmDialog
          isOpen={true}
          title="Delete Recurring Expense"
          message="Are you sure you want to delete this recurring expense? This will also remove all upcoming payment reminders."
          confirmLabel="Delete"
          cancelLabel="Cancel"
          variant="danger"
          onConfirm={() => handleDeleteRecurring(deleteConfirmId)}
          onCancel={() => setDeleteConfirmId(null)}
          isLoading={isDeleting}
        />
      )}
    </>
  );
};
