import { useNavigate } from 'react-router-dom';
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip } from 'recharts';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/Card/Card';
import { Button } from '@/components/Button/Button';
import { Loading, EmptyState, ErrorState } from '@/components/Loading/Loading';
import { ActionMenu } from '@/components/ActionMenu/ActionMenu';
import { AddExpenseModal } from '@/components/AddExpenseModal/AddExpenseModal';
import { ImportCSVModal } from '@/components/ImportCSV/ImportCSVModal';
import { useApi } from '@/hooks/useApi';
import { dashboardApi } from '@/services/dashboard.api';
import { formatCurrency, formatDate, truncateText } from '@/utils/helpers';
import styles from './Dashboard.module.css';
import { useState } from 'react';

const GRADIENT_COLORS = [
  { start: '#818cf8', end: '#6366f1' },
  { start: '#f0abfc', end: '#ec4899' },
  { start: '#fcd34d', end: '#f59e0b' },
  { start: '#6ee7b7', end: '#10b981' },
  { start: '#c084fc', end: '#8b5cf6' },
  { start: '#67e8f9', end: '#06b6d4' },
  { start: '#fca5a5', end: '#ef4444' },
  { start: '#5eead4', end: '#14b8a6' },
  { start: '#fdba74', end: '#f97316' },
];

// Helper to get category icon
function getCategoryIcon(category: string): string {
  const categoryIcons: Record<string, string> = {
    food: '🍔',
    transport: '🚗',
    shopping: '🛍️',
    bills: '📱',
    entertainment: '🎬',
  };
  return categoryIcons[category] || '💰';
}

// Custom label for pie chart with amount and percentage
const renderCustomLabel = ({ cx, cy, midAngle, innerRadius, outerRadius, percent, value }: any) => {
  const RADIAN = Math.PI / 180;
  const radius = innerRadius + (outerRadius - innerRadius) * 0.5;
  const x = cx + radius * Math.cos(-midAngle * RADIAN);
  const y = cy + radius * Math.sin(-midAngle * RADIAN);

  if (percent < 0.05) return null; // Don't show label for slices < 5%

  return (
    <g>
      <text
        x={x}
        y={y - 8}
        fill="white"
        textAnchor="middle"
        dominantBaseline="central"
        style={{
          fontSize: '13px',
          fontWeight: 700,
          textShadow: '0 2px 4px rgba(0, 0, 0, 0.6)',
        }}
      >
        {formatCurrency(value)}
      </text>
      <text
        x={x}
        y={y + 8}
        fill="white"
        textAnchor="middle"
        dominantBaseline="central"
        style={{
          fontSize: '12px',
          fontWeight: 600,
          textShadow: '0 2px 4px rgba(0, 0, 0, 0.6)',
          opacity: 0.95,
        }}
      >
        {`${(percent * 100).toFixed(1)}%`}
      </text>
    </g>
  );
};

export const DashboardPage = () => {
  const navigate = useNavigate();
  const [activeIndex, setActiveIndex] = useState<number | null>(null);
  const [showAddExpenseModal, setShowAddExpenseModal] = useState(false);
  const [showImportCSVModal, setShowImportCSVModal] = useState(false);

  // Mock function for upcoming payments - replace with real API call
  const getUpcomingPayments = () => {
    const mockPayments = [
      {
        id: '1',
        description: 'Netflix Subscription',
        category: 'entertainment',
        amount: 15.99,
        dueDate: 'Due in 2 days',
        frequency: 'Monthly',
        status: 'due-soon' as const,
      },
      {
        id: '2',
        description: 'Internet Bill',
        category: 'bills',
        amount: 79.99,
        dueDate: 'Due in 5 days',
        frequency: 'Monthly',
        status: 'upcoming' as const,
      },
      {
        id: '3',
        description: 'Gym Membership',
        category: 'shopping',
        amount: 49.99,
        dueDate: 'Due in 7 days',
        frequency: 'Monthly',
        status: 'upcoming' as const,
      },
      {
        id: '4',
        description: 'Car Insurance',
        category: 'transport',
        amount: 120.00,
        dueDate: 'Due today',
        frequency: 'Monthly',
        status: 'overdue' as const,
      },
      {
        id: '5',
        description: 'Phone Bill',
        category: 'bills',
        amount: 55.00,
        dueDate: 'Due tomorrow',
        frequency: 'Monthly',
        status: 'due-soon' as const,
      },
    ];
    return mockPayments;
  };

  const { data: summary, isLoading, error, refetch } = useApi(
    () => dashboardApi.getSummary(),
    { immediate: true }
  );

  if (isLoading) {
    return <Loading text="Loading dashboard..." />;
  }

  if (error) {
    return <ErrorState description={error} onRetry={refetch} />;
  }

  if (!summary) {
    return <EmptyState title="No data available" description="Start adding expenses to see your dashboard" />;
  }

  // Map backend data to frontend expectations
  const totalSpent = (summary as any).totalExpenses || 0;
  const totalIncome = (summary as any).totalIncome || 0;
  const savings = (summary as any).savings || totalIncome - totalSpent;
  const categoryBreakdown = (summary as any).categoryBreakdown || [];
  const recentTransactions = (summary as any).recentTransactions || [];

  // Calculate status
  const getStatus = (): 'safe' | 'warning' | 'risk' => {
    if (totalIncome === 0) return 'safe';
    const percentageSpent = (totalSpent / totalIncome) * 100;
    if (percentageSpent > 100) return 'risk';
    if (percentageSpent > 80) return 'warning';
    return 'safe';
  };

  const status = getStatus();

  const getStatusLabel = (status: 'safe' | 'warning' | 'risk') => {
    const labels = {
      safe: '✓ On Track',
      warning: '⚠ Caution',
      risk: '⚡ At Risk',
    };
    return labels[status];
  };

  // Data for top categories pie chart
  const chartData = categoryBreakdown.map((cat: any) => ({
    name: cat.category,
    value: Number(cat.amount),
    percentage: cat.percentage,
  }));

  return (
    <div className={styles.dashboard}>
      {/* Header */}
      <div className={styles['dashboard-header']}>
        <div>
          <h1 className={styles['dashboard-title']}>Dashboard</h1>
          <p className={styles['dashboard-date']}>{formatDate(new Date(), 'long')}</p>
        </div>
        <ActionMenu 
          onAddExpense={() => setShowAddExpenseModal(true)}
          onImportCSV={() => setShowImportCSVModal(true)}
        />
      </div>

      {/* Summary Cards */}
      <div className={styles['summary-grid']}>
        <Card>
          <div className={styles['summary-card']}>
            <div className={styles['summary-card-header']}>
              <span className={styles['summary-label']}>Total Spent</span>
              <span className={styles['summary-icon']}>💰</span>
            </div>
            <div className={styles['summary-value']}>
              {formatCurrency(totalSpent)}
            </div>
            <span className={`${styles['summary-status']} ${styles[status]}`}>
              {getStatusLabel(status)}
            </span>
          </div>
        </Card>

        <Card>
          <div className={styles['summary-card']}>
            <div className={styles['summary-card-header']}>
              <span className={styles['summary-label']}>Total Income</span>
              <span className={styles['summary-icon']}>📈</span>
            </div>
            <div className={styles['summary-value']}>
              {formatCurrency(totalIncome)}
            </div>
          </div>
        </Card>

        <Card>
          <div className={styles['summary-card']}>
            <div className={styles['summary-card-header']}>
              <span className={styles['summary-label']}>Savings</span>
              <span className={styles['summary-icon']}>💎</span>
            </div>
            <div className={styles['summary-value']}>
              {formatCurrency(savings)}
            </div>
          </div>
        </Card>

        <Card>
          <div className={styles['summary-card']}>
            <div className={styles['summary-card-header']}>
              <span className={styles['summary-label']}>Save Rate</span>
              <span className={styles['summary-icon']}>📊</span>
            </div>
            <div className={styles['summary-value']}>
              {totalIncome > 0 ? ((savings / totalIncome) * 100).toFixed(1) : 0}%
            </div>
          </div>
        </Card>
      </div>

      {/* Charts */}
      <div className={styles['charts-section']}>
        <Card>
          <CardHeader>
            <CardTitle>Spending by Category</CardTitle>
          </CardHeader>
          <CardContent>
            <div className={styles['chart-container']}>
              {chartData.length > 0 ? (
                <ResponsiveContainer width="100%" height={450}>
                  <PieChart>
                    <defs>
                      {GRADIENT_COLORS.map((gradient, index) => (
                        <linearGradient key={`gradient-${index}`} id={`gradient-${index}`} x1="0" y1="0" x2="0" y2="1">
                          <stop offset="0%" stopColor={gradient.start} stopOpacity={0.9} />
                          <stop offset="100%" stopColor={gradient.end} stopOpacity={1} />
                        </linearGradient>
                      ))}
                    </defs>
                    <Pie
                      data={chartData}
                      cx="50%"
                      cy="50%"
                      innerRadius={80}
                      outerRadius={140}
                      paddingAngle={3}
                      dataKey="value"
                      label={renderCustomLabel}
                      labelLine={false}
                      onMouseEnter={(_, index) => setActiveIndex(index)}
                      onMouseLeave={() => setActiveIndex(null)}
                      animationBegin={0}
                      animationDuration={800}
                      animationEasing="ease-out"
                    >
                      {chartData.map((_: any, index: number) => (
                        <Cell 
                          key={`cell-${index}`} 
                          fill={`url(#gradient-${index % GRADIENT_COLORS.length})`}
                          stroke="#fff"
                          strokeWidth={2}
                          style={{
                            filter: activeIndex === index ? 'drop-shadow(0 4px 6px rgba(0, 0, 0, 0.3))' : 'none',
                            transform: activeIndex === index ? 'scale(1.05)' : 'scale(1)',
                            transformOrigin: 'center',
                            transition: 'all 0.3s ease',
                          }}
                        />
                      ))}
                    </Pie>
                    <Tooltip 
                      formatter={(value: any, name: string) => [formatCurrency(Number(value)), name]}
                      contentStyle={{
                        backgroundColor: 'rgba(255, 255, 255, 0.95)',
                        border: 'none',
                        borderRadius: '8px',
                        boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
                        padding: '12px',
                      }}
                      labelStyle={{ fontWeight: 600, marginBottom: '4px' }}
                    />
                    <Legend 
                      verticalAlign="bottom" 
                      height={36}
                      iconType="circle"
                      wrapperStyle={{ paddingTop: '20px' }}
                    />
                  </PieChart>
                </ResponsiveContainer>
              ) : (
                <EmptyState
                  icon="📊"
                  title="No expense data"
                  description="Add expenses to see category breakdown"
                />
              )}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Upcoming Payments</CardTitle>
          </CardHeader>
          <CardContent>
            <div className={styles['recurring-section']}>
              {/* Mock recurring payments - to be replaced with real API data */}
              {getUpcomingPayments().length > 0 ? (
                getUpcomingPayments().map((payment) => (
                  <div key={payment.id} className={styles['recurring-item']}>
                    <div className={styles['recurring-icon']}>
                      {getCategoryIcon(payment.category)}
                    </div>
                    <div className={styles['recurring-details']}>
                      <div className={styles['recurring-title']}>
                        {payment.description}
                        <span className={`${styles['recurring-badge']} ${styles[payment.status]}`}>
                          {payment.status === 'due-soon' ? 'Due Soon' : payment.status === 'overdue' ? 'Overdue' : 'Upcoming'}
                        </span>
                      </div>
                      <div className={styles['recurring-meta']}>
                        <span>{payment.category}</span>
                        <span>•</span>
                        <span>{payment.dueDate}</span>
                        <span>•</span>
                        <span>{payment.frequency}</span>
                      </div>
                    </div>
                    <div className={styles['recurring-amount']}>
                      {formatCurrency(payment.amount)}
                    </div>
                  </div>
                ))
              ) : (
                <div className={styles['empty-recurring']}>
                  <div className={styles['empty-recurring-icon']}>📅</div>
                  <div style={{ fontSize: 'var(--font-size-base)', fontWeight: 600, marginBottom: '4px' }}>
                    No upcoming payments
                  </div>
                  <div style={{ fontSize: 'var(--font-size-sm)' }}>
                    Set up recurring expenses to track upcoming bills
                  </div>
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Recent Transactions */}
      <Card>
        <div className={styles['transactions-header']}>
          <CardTitle>Recent Transactions</CardTitle>
        </div>
        <CardContent>
          {recentTransactions && recentTransactions.length > 0 ? (
            <div>
              {recentTransactions.slice(0, 10).map((transaction: any) => (
                <div key={transaction.id} className={styles['transaction-item']}>
                  <div
                    className={styles['transaction-icon']}
                    style={{ backgroundColor: 'rgba(99, 102, 241, 0.1)' }}
                  >
                    {getCategoryIcon(transaction.category)}
                  </div>
                  <div className={styles['transaction-details']}>
                    <div className={styles['transaction-description']} title={transaction.description}>
                      {truncateText(transaction.description, 50)}
                    </div>
                    <div className={styles['transaction-meta']}>
                      {transaction.category} • {formatDate(transaction.date, 'relative')} • {transaction.paymentMethod}
                    </div>
                  </div>
                  <div className={styles['transaction-amount']}>
                    {formatCurrency(transaction.amount)}
                  </div>
                </div>
              ))}
              <Button
                variant="ghost"
                fullWidth
                onClick={() => navigate('/expenses')}
                style={{ marginTop: 'var(--spacing-md)' }}
              >
                View All Transactions
              </Button>
            </div>
          ) : (
            <EmptyState
              icon="💸"
              title="No transactions yet"
              description="Start tracking your expenses"
              action={
                <Button onClick={() => navigate('/expenses/add')}>Add First Expense</Button>
              }
            />
          )}
        </CardContent>
      </Card>

      {/* Modals */}
      <AddExpenseModal 
        isOpen={showAddExpenseModal}
        onClose={() => setShowAddExpenseModal(false)}
        onSuccess={() => {
          setShowAddExpenseModal(false);
          refetch(); // Refresh dashboard after adding expense
        }}
      />
      
      <ImportCSVModal
        isOpen={showImportCSVModal}
        onClose={() => setShowImportCSVModal(false)}
        onSuccess={() => {
          setShowImportCSVModal(false);
          refetch(); // Refresh dashboard after importing
        }}
      />
    </div>
  );
};
