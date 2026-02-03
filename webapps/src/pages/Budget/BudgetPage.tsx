import { useState, useEffect } from 'react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/Card/Card';
import { Button } from '@/components/Button/Button';
import { Input } from '@/components/Input/Input';
import { Select } from '@/components/Select/Select';
import { EmptyState, Loading, ErrorState } from '@/components/Loading/Loading';
import { budgetApi } from '@/services/budget.api';
import { expenseApi } from '@/services/expense.api';
import { formatCurrency } from '@/utils/helpers';
import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip } from 'recharts';
import type { Budget } from '@/types';
import styles from './BudgetPage.module.css';

interface BudgetModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  editBudget?: Budget | null;
}

const BudgetModal = ({ isOpen, onClose, onSuccess, editBudget }: BudgetModalProps) => {
  const [category, setCategory] = useState('');
  const [plannedAmount, setPlannedAmount] = useState('');
  const [month, setMonth] = useState(new Date().toISOString().slice(0, 7));
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (editBudget) {
      setCategory(editBudget.category);
      setPlannedAmount(editBudget.limit.toString());
      setMonth(new Date(editBudget.startDate).toISOString().slice(0, 7));
    } else {
      setCategory('');
      setPlannedAmount('');
      setMonth(new Date().toISOString().slice(0, 7));
    }
  }, [editBudget, isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const budgetData = {
        category: category,
        limit: parseFloat(plannedAmount),
        period: 'monthly',
        startDate: new Date(month + '-01').toISOString(),
        endDate: new Date(new Date(month + '-01').getFullYear(), new Date(month + '-01').getMonth() + 1, 0).toISOString(),
        alertThreshold: 80,
      };

      const response = editBudget
        ? await budgetApi.updateBudget(editBudget.id, budgetData)
        : await budgetApi.upsertBudget(budgetData);

      if (response.success) {
        onSuccess();
        onClose();
      } else {
        setError(response.error || 'Failed to save budget');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save budget');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className={styles.modalOverlay} onClick={onClose}>
      <div className={styles.modalContainer} onClick={(e) => e.stopPropagation()}>
        <div className={styles.modalHeader}>
          <h2 className={styles.modalTitle}>{editBudget ? 'Edit Budget' : 'Add Budget'}</h2>
          <button className={styles.closeButton} onClick={onClose}>✕</button>
        </div>

        {error && <div className={styles.error}>{error}</div>}

        <form onSubmit={handleSubmit} className={styles.modalForm}>
          <Select
            label="Category"
            options={[
              { value: 'food', label: '🍔 Food & Dining' },
              { value: 'transport', label: '🚗 Transportation' },
              { value: 'shopping', label: '🛍️ Shopping' },
              { value: 'bills', label: '📱 Bills & Utilities' },
              { value: 'entertainment', label: '🎬 Entertainment' },
            ]}
            value={category}
            onChange={(e) => setCategory(e.target.value)}
            required
          />

          <Input
            label="Planned Amount"
            type="number"
            value={plannedAmount}
            onChange={(e) => setPlannedAmount(e.target.value)}
            placeholder="0.00"
            required
            icon="₹"
          />

          <Input
            label="Month"
            type="month"
            value={month}
            onChange={(e) => setMonth(e.target.value)}
            required
          />

          <div className={styles.modalActions}>
            <Button type="button" variant="ghost" onClick={onClose} disabled={loading}>
              Cancel
            </Button>
            <Button type="submit" disabled={loading}>
              {loading ? 'Saving...' : editBudget ? 'Update Budget' : 'Add Budget'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};

interface ChartModalProps {
  isOpen: boolean;
  onClose: () => void;
  budgets: Budget[];
  month: string;
}

const ChartModal = ({ isOpen, onClose, budgets, month }: ChartModalProps) => {
  if (!isOpen) return null;

  const chartData = budgets.map(budget => ({
    name: budget.category,
    value: budget.spent,
    planned: budget.limit,
  }));

  const COLORS = ['#6366f1', '#ec4899', '#f59e0b', '#10b981', '#8b5cf6', '#06b6d4'];

  return (
    <div className={styles.modalOverlay} onClick={onClose}>
      <div className={styles.chartModalContainer} onClick={(e) => e.stopPropagation()}>
        <div className={styles.modalHeader}>
          <h2 className={styles.modalTitle}>Budget Overview - {month}</h2>
          <button className={styles.closeButton} onClick={onClose}>✕</button>
        </div>

        <div className={styles.chartContent}>
          <ResponsiveContainer width="100%" height={300}>
            <PieChart>
              <Pie
                data={chartData}
                dataKey="value"
                nameKey="name"
                cx="50%"
                cy="50%"
                outerRadius={100}
                label={(entry) => `${entry.name}: ${formatCurrency(entry.value)}`}
              >
                {chartData.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip formatter={(value: number) => formatCurrency(value)} />
            </PieChart>
          </ResponsiveContainer>

          <div className={styles.chartLegend}>
            {chartData.map((item, index) => (
              <div key={index} className={styles.legendItem}>
                <div className={styles.legendColor} style={{ backgroundColor: COLORS[index % COLORS.length] }}></div>
                <div className={styles.legendText}>
                  <span className={styles.legendName}>{item.name}</span>
                  <span className={styles.legendValue}>
                    {formatCurrency(item.value)} / {formatCurrency(item.planned)}
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};

export const BudgetPage = () => {
  const [budgets, setBudgets] = useState<Budget[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showAddModal, setShowAddModal] = useState(false);
  const [showChartModal, setShowChartModal] = useState(false);
  const [editingBudget, setEditingBudget] = useState<Budget | null>(null);
  const [selectedMonth, setSelectedMonth] = useState('');

  const currentMonth = new Date().toISOString().slice(0, 7);

  const fetchBudgets = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await budgetApi.getBudgets();
      if (response.success && response.data) {
        setBudgets(response.data);
      } else {
        setError(response.error || 'Failed to load budgets');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load budgets');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchBudgets();
  }, []);

  const currentMonthBudgets = budgets.filter(b => {
    const budgetMonth = new Date(b.startDate).toISOString().slice(0, 7);
    return budgetMonth === currentMonth;
  });
  
  const previousMonths = [...new Set(budgets.filter(b => {
    const budgetMonth = new Date(b.startDate).toISOString().slice(0, 7);
    return budgetMonth < currentMonth;
  }).map(b => new Date(b.startDate).toISOString().slice(0, 7)))].sort().reverse();

  const calculateTotals = (monthBudgets: Budget[]) => {
    const totalBudget = monthBudgets.reduce((sum, b) => sum + b.limit, 0);
    const totalSpent = monthBudgets.reduce((sum, b) => sum + b.spent, 0);
    const remaining = totalBudget - totalSpent;
    return { totalBudget, totalSpent, remaining };
  };

  const currentTotals = calculateTotals(currentMonthBudgets);

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this budget?')) return;
    try {
      const response = await budgetApi.deleteBudget(id);
      if (response.success) {
        fetchBudgets();
      }
    } catch (err) {
      alert('Failed to delete budget');
    }
  };

  const getCategoryIcon = (categoryId: string) => {
    const icons: Record<string, string> = {
      food: '🍔',
      transport: '🚗',
      shopping: '🛍️',
      bills: '📱',
      entertainment: '🎬',
    };
    return icons[categoryId] || '💰';
  };

  if (loading) {
    return <Loading text="Loading budgets..." />;
  }

  return (
    <div>
      <div className={styles.header}>
        <div>
          <h1 className={styles.title}>Budget Planner</h1>
          <p className={styles.subtitle}>Track your monthly budgets by category</p>
        </div>
        <Button onClick={() => setShowAddModal(true)}>+ Add Budget</Button>
      </div>

      {/* Current Month Overview */}
      <Card style={{ marginBottom: '1.5rem' }}>
        <CardHeader>
          <div className={styles.cardHeaderWithIcon}>
            <CardTitle>Current Month - {new Date(currentMonth + '-01').toLocaleDateString('en-US', { month: 'long', year: 'numeric' })}</CardTitle>
            {currentMonthBudgets.length > 0 && (
              <button 
                className={styles.chartIcon}
                onClick={() => { setSelectedMonth(currentMonth); setShowChartModal(true); }}
                title="View spending chart"
              >
                📊
              </button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {currentMonthBudgets.length === 0 ? (
            <EmptyState
              icon="🎯"
              title="No budgets for this month"
              description="Create category-wise budgets to track your spending"
              action={<Button onClick={() => setShowAddModal(true)}>Create Budget</Button>}
            />
          ) : (
            <>
              <div className={styles.summaryGrid}>
                <div className={styles.summaryCard}>
                  <div className={styles.summaryLabel}>Total Budget</div>
                  <div className={styles.summaryValue}>{formatCurrency(currentTotals.totalBudget)}</div>
                </div>
                <div className={styles.summaryCard}>
                  <div className={styles.summaryLabel}>Spent</div>
                  <div className={`${styles.summaryValue} ${styles.warning}`}>{formatCurrency(currentTotals.totalSpent)}</div>
                </div>
                <div className={styles.summaryCard}>
                  <div className={styles.summaryLabel}>Remaining</div>
                  <div className={`${styles.summaryValue} ${currentTotals.remaining >= 0 ? styles.success : styles.danger}`}>
                    {formatCurrency(currentTotals.remaining)}
                  </div>
                </div>
              </div>

              <div className={styles.budgetList}>
                {currentMonthBudgets.map(budget => {
                  const percentage = (budget.spent / budget.limit) * 100;
                  const status = percentage > 100 ? 'danger' : percentage > 80 ? 'warning' : 'success';

                  return (
                    <div key={budget.id} className={styles.budgetItem}>
                      <div className={styles.budgetIcon}>
                        {getCategoryIcon(budget.category)}
                      </div>
                      <div className={styles.budgetDetails}>
                        <div className={styles.budgetName}>
                          {budget.category}
                        </div>
                        <div className={styles.progressBar}>
                          <div 
                            className={`${styles.progressFill} ${styles[status]}`}
                            style={{ width: `${Math.min(percentage, 100)}%` }}
                          ></div>
                        </div>
                        <div className={styles.budgetMeta}>
                          {formatCurrency(budget.spent)} / {formatCurrency(budget.limit)} 
                          <span className={styles[status]}> ({percentage.toFixed(0)}%)</span>
                        </div>
                      </div>
                      <div className={styles.budgetActions}>
                        <button 
                          className={styles.actionButton}
                          onClick={() => { setEditingBudget(budget); setShowAddModal(true); }}
                          title="Edit"
                        >
                          ✏️
                        </button>
                        <button 
                          className={styles.actionButton}
                          onClick={() => handleDelete(budget.id)}
                          title="Delete"
                        >
                          🗑️
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>
            </>
          )}
        </CardContent>
      </Card>

      {/* Previous Months */}
      {previousMonths.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Previous Months</CardTitle>
          </CardHeader>
          <CardContent>
            <div className={styles.previousMonthsList}>
              {previousMonths.map(month => {
                const monthBudgets = budgets.filter(b => new Date(b.startDate).toISOString().slice(0, 7) === month);
                const totals = calculateTotals(monthBudgets);
                const monthDate = new Date(month + '-01');
                const monthLabel = monthDate.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });

                return (
                  <div key={month} className={styles.previousMonthItem}>
                    <div className={styles.monthHeader}>
                      <div className={styles.monthLabel}>{monthLabel}</div>
                      <button 
                        className={styles.chartIconSmall}
                        onClick={() => { setSelectedMonth(month); setShowChartModal(true); }}
                        title="View chart"
                      >
                        📊
                      </button>
                    </div>
                    <div className={styles.monthStats}>
                      <div className={styles.statItem}>
                        <span className={styles.statLabel}>Budget:</span>
                        <span className={styles.statValue}>{formatCurrency(totals.totalBudget)}</span>
                      </div>
                      <div className={styles.statItem}>
                        <span className={styles.statLabel}>Spent:</span>
                        <span className={styles.statValue}>{formatCurrency(totals.totalSpent)}</span>
                      </div>
                      <div className={styles.statItem}>
                        <span className={styles.statLabel}>Status:</span>
                        <span className={`${styles.statValue} ${totals.remaining >= 0 ? styles.success : styles.danger}`}>
                          {totals.remaining >= 0 ? '✓ Under Budget' : '⚠ Over Budget'}
                        </span>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Modals */}
      <BudgetModal
        isOpen={showAddModal}
        onClose={() => { setShowAddModal(false); setEditingBudget(null); }}
        onSuccess={fetchBudgets}
        editBudget={editingBudget}
      />

      <ChartModal
        isOpen={showChartModal}
        onClose={() => setShowChartModal(false)}
        budgets={budgets.filter(b => new Date(b.startDate).toISOString().slice(0, 7) === selectedMonth)}
        month={selectedMonth}
      />
    </div>
  );
};
