import { useNavigate, useParams } from 'react-router-dom';
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip, BarChart, Bar, XAxis, YAxis } from 'recharts';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/Card/Card';
import { Button } from '@/components/Button/Button';
import { Loading, EmptyState, ErrorState } from '@/components/Loading/Loading';
import { ImportCSVModal } from '@/components/ImportCSV/ImportCSVModal';
import { DateRangePicker } from '@/components/DateRangePicker/DateRangePicker';
import { PaymentConfirmationModal } from '@/components/PaymentConfirmationModal/PaymentConfirmationModal';
import ToastContainer from '@/components/Toast/ToastContainer';
import { useApi } from '@/hooks/useApi';
import { useToast } from '@/hooks/useToast';
import { dashboardApi } from '@/services/dashboard.api';
import { formatCurrency, formatDate } from '@/utils/helpers';
import type { UpcomingPayment } from '@/types';
import styles from './Dashboard.module.css';
import { useState, useEffect, useRef, useCallback } from 'react';

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

// Helper to get category color for charts
function getCategoryColor(category: string): string {
  const categoryColors: Record<string, string> = {
    food: '#6366f1',
    transport: '#ef4444',
    shopping: '#f97316',
    bills: '#8b5cf6',
    entertainment: '#ec4899',
  };
  return categoryColors[category] || '#6366f1';
}

// Custom label for pie chart with amount and percentage
const renderCustomLabel = ({ cx, cy, midAngle, innerRadius, outerRadius, percent, value }: any) => {
  const RADIAN = Math.PI / 180;
  const radius = innerRadius + (outerRadius - innerRadius) * 0.5;
  const x = cx + radius * Math.cos(-midAngle * RADIAN);
  const y = cy + radius * Math.sin(-midAngle * RADIAN);

  if (percent < 0.05) return null; // Don't show label for slices < 5%

  return (
    <g pointerEvents="none">
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
          pointerEvents: 'none',
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
          pointerEvents: 'none',
        }}
      >
        {`${(percent * 100).toFixed(1)}%`}
      </text>
    </g>
  );
};

export const DashboardPage = () => {
  const navigate = useNavigate();
  const { toasts, dismissToast, success: showSuccess, error: showError } = useToast();
  const [showImportCSVModal, setShowImportCSVModal] = useState(false);
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [showDateRangePicker, setShowDateRangePicker] = useState(false);
  const filterButtonRef = useRef<HTMLButtonElement>(null);

  // Upcoming payments state
  const [upcomingPayments, setUpcomingPayments] = useState<UpcomingPayment[]>([]);
  const [upcomingTotal, setUpcomingTotal] = useState(0);
  const [upcomingPage, setUpcomingPage] = useState(1);
  const [upcomingPageSize] = useState(5);
  const [upcomingHasMore, setUpcomingHasMore] = useState(false);
  const [upcomingLoading, setUpcomingLoading] = useState(true);
  
  // Payment modal state
  const [selectedPayment, setSelectedPayment] = useState<UpcomingPayment | null>(null);
  const [isPaymentModalOpen, setIsPaymentModalOpen] = useState(false);
  const [isPaymentModalLoading, setIsPaymentModalLoading] = useState(false);

  // Extract bookId from URL params
  const { bookId } = useParams<{ bookId: string }>();

  useEffect(() => {
    if (!bookId) {
      // Redirect to expense books page if no bookId
      navigate('/');
    }
  }, [bookId, navigate]);

  const handleDateRangeChange = (start: string, end: string) => {
    setStartDate(start);
    setEndDate(end);
  };

  // Fetch upcoming payments
  const fetchUpcomingPayments = useCallback(async (page: number = 1) => {
    if (!bookId) return;
    setUpcomingLoading(true);
    try {
      const response = await dashboardApi.getUpcomingPayments(bookId, page, upcomingPageSize);
      if (response.success && response.data) {
        setUpcomingPayments(response.data.items || []);
        setUpcomingTotal(response.data.total || 0);
        setUpcomingHasMore(response.data.hasMore || false);
        setUpcomingPage(page);
      }
    } catch (err) {
      console.error('Failed to fetch upcoming payments:', err);
    } finally {
      setUpcomingLoading(false);
    }
  }, [upcomingPageSize, bookId]);

  // Handle marking upcoming payment as paid
  const handleConfirmPayment = async (paidDate: string, recordAsExpense: boolean) => {
    if (!selectedPayment) return;

    setIsPaymentModalLoading(true);
    try {
      const response = await dashboardApi.markUpcomingPaymentAsPaid(
        selectedPayment.id,
        paidDate,
        recordAsExpense
      );
      if (response.success) {
        const expenseMsg = recordAsExpense ? ' and recorded as expense' : '';
        showSuccess(`Payment confirmed${expenseMsg}!`);
        setIsPaymentModalOpen(false);
        setSelectedPayment(null);
        fetchUpcomingPayments(upcomingPage);
        refetch(); // Refresh dashboard summary
        fetchGroupedTransactions(); // Refresh transactions
      } else {
        showError(response.error || 'Failed to record payment');
      }
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to record payment');
    } finally {
      setIsPaymentModalLoading(false);
    }
  };

  const getStatusBadgeClass = (status: string) => {
    switch (status) {
      case 'overdue': return 'overdue';
      case 'pending': return 'overdue'; // pending uses same style as overdue
      case 'due': return 'due-soon';
      case 'upcoming': return 'upcoming';
      default: return 'upcoming';
    }
  };

  const getPaymentStatusLabel = (status: string) => {
    switch (status) {
      case 'overdue': return 'Overdue';
      case 'pending': return 'Pending';
      case 'due': return 'Due Today';
      case 'upcoming': return 'Upcoming';
      default: return status;
    }
  };

  const { data: summary, isLoading, error, refetch } = useApi(
    () => dashboardApi.getSummary(bookId || undefined),
    { immediate: !!bookId }
  );

  const { 
    data: groupedTransactions, 
    isLoading: loadingGrouped, 
    execute: fetchGroupedTransactions 
  } = useApi(
    () => dashboardApi.getGroupedTransactions(bookId || undefined, startDate || undefined, endDate || undefined),
    { immediate: !!bookId }
  );

  // Refetch grouped transactions when date filters change
  useEffect(() => {
    if (bookId) {
      fetchGroupedTransactions();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [startDate, endDate, bookId]);

  // Fetch upcoming payments on mount
  useEffect(() => {
    if (bookId) {
      fetchUpcomingPayments(1);
    }
  }, [fetchUpcomingPayments, bookId]);

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

  // Calculate status
  const getStatus = (): 'safe' | 'warning' | 'risk' => {
    if (totalIncome === 0) return 'safe';
    const percentageSpent = (totalSpent / totalIncome) * 100;
    if (percentageSpent > 100) return 'risk';
    if (percentageSpent > 80) return 'warning';
    return 'safe';
  };

  const status = getStatus();

  const getSummaryStatusLabel = (status: 'safe' | 'warning' | 'risk') => {
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
        <Button variant="primary" onClick={() => setShowImportCSVModal(true)}>
          <i className="fa-solid fa-plus" style={{ marginRight: '0.5rem' }}></i>
          Add Expense
        </Button>
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
              {getSummaryStatusLabel(status)}
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
                      animationBegin={0}
                      animationDuration={800}
                      animationEasing="ease-out"
                      isAnimationActive={true}
                    >
                      {chartData.map((_: any, index: number) => (
                        <Cell 
                          key={`cell-${index}`} 
                          fill={`url(#gradient-${index % GRADIENT_COLORS.length})`}
                          stroke="#fff"
                          strokeWidth={2}
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
                      isAnimationActive={false}
                      animationDuration={0}
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
              {upcomingLoading ? (
                <Loading text="Loading upcoming payments..." />
              ) : upcomingPayments.length > 0 ? (
                <>
                  {upcomingPayments.map((payment) => (
                    <div key={payment.id} className={styles['recurring-item']}>
                      <div className={styles['recurring-icon']}>
                        {getCategoryIcon(payment.category)}
                      </div>
                      <div className={styles['recurring-details']}>
                        <div className={styles['recurring-title']}>
                          {payment.description || payment.category}
                          <span className={`${styles['recurring-badge']} ${styles[getStatusBadgeClass(payment.status)]}`}>
                            {getPaymentStatusLabel(payment.status)}
                          </span>
                        </div>
                        <div className={styles['recurring-meta']}>
                          <span>{payment.category}</span>
                          <span>•</span>
                          <span>{payment.dueDateLabel}</span>
                          <span>•</span>
                          <span style={{ textTransform: 'capitalize' }}>{payment.frequency}</span>
                        </div>
                      </div>
                      <div className={styles['recurring-action']}>
                        <div className={styles['recurring-amount']}>
                          {formatCurrency(payment.amount)}
                        </div>
                        <button
                          className={styles['pay-button']}
                          onClick={() => {
                            setSelectedPayment(payment);
                            setIsPaymentModalOpen(true);
                          }}
                        >
                          <i className="fa-solid fa-check"></i>
                          <span>Pay</span>
                        </button>
                      </div>
                    </div>
                  ))}
                  {/* Pagination */}
                  {upcomingTotal > upcomingPageSize && (
                    <div className={styles['upcoming-pagination']}>
                      <Button
                        variant="ghost"
                        onClick={() => fetchUpcomingPayments(upcomingPage - 1)}
                        disabled={upcomingPage <= 1}
                      >
                        <i className="fa-solid fa-chevron-left"></i>
                      </Button>
                      <span className={styles['page-info']}>
                        Page {upcomingPage} of {Math.ceil(upcomingTotal / upcomingPageSize)}
                      </span>
                      <Button
                        variant="ghost"
                        onClick={() => fetchUpcomingPayments(upcomingPage + 1)}
                        disabled={!upcomingHasMore}
                      >
                        <i className="fa-solid fa-chevron-right"></i>
                      </Button>
                    </div>
                  )}
                </>
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
          <div className={styles['filter-section']}>
            {startDate && endDate && (
              <div className={styles['date-chip']}>
                {new Date(startDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} - {new Date(endDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                <button 
                  className={styles['chip-close']}
                  onClick={() => {
                    setStartDate('');
                    setEndDate('');
                  }}
                  aria-label="Clear date filter"
                >
                  ×
                </button>
              </div>
            )}
            <button
              ref={filterButtonRef}
              className={styles['filter-button']}
              onClick={() => setShowDateRangePicker(!showDateRangePicker)}
              aria-label="Filter transactions"
              title="Filter by date range"
            >
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
                <line x1="16" y1="2" x2="16" y2="6"/>
                <line x1="8" y1="2" x2="8" y2="6"/>
                <line x1="3" y1="10" x2="21" y2="10"/>
              </svg>
            </button>
          </div>
        </div>
        <CardContent>
          {loadingGrouped ? (
            <Loading text="Loading transactions..." />
          ) : groupedTransactions && groupedTransactions.length > 0 ? (
            <div>
              {groupedTransactions.map((group: any) => {
                // Prepare data for bar chart
                const chartData = group.categorySpending.map((cat: any) => ({
                  name: cat.category,
                  amount: cat.amount,
                  percentage: ((cat.amount / group.totalSpent) * 100).toFixed(1),
                  count: cat.count,
                  fill: getCategoryColor(cat.category),
                }));

                return (
                  <div key={group.date} className={styles['date-group']}>
                    <div className={styles['date-header']}>
                      <span className={styles['date-label']}>{group.dateLabel}</span>
                      <span className={styles['date-total']}>{formatCurrency(group.totalSpent)}</span>
                    </div>
                    <div className={styles['chart-wrapper']}>
                      <ResponsiveContainer width="100%" height={group.categorySpending.length * 45 + 30}>
                        <BarChart
                          data={chartData}
                          layout="vertical"
                          margin={{ top: 8, right: 15, left: 90, bottom: 8 }}
                          barSize={24}
                        >
                          <XAxis 
                            type="number" 
                            stroke="#9ca3af"
                            fontSize={12}
                            axisLine={{ stroke: '#e5e7eb' }}
                            tickLine={{ stroke: '#e5e7eb' }}
                          />
                          <YAxis 
                            dataKey="name" 
                            type="category"
                            width={85}
                            tick={{ fontSize: 12, fill: '#4b5563' }}
                            axisLine={false}
                            tickLine={false}
                          />
                          <Tooltip
                            contentStyle={{
                              backgroundColor: 'rgba(255, 255, 255, 0.98)',
                              border: '1px solid #e5e7eb',
                              borderRadius: '8px',
                              padding: '10px 14px',
                              color: '#1f2937',
                              boxShadow: '0 4px 12px rgba(0, 0, 0, 0.1)',
                            }}
                            labelStyle={{ color: '#1f2937', fontWeight: 600 }}
                            formatter={(value: any, name: string, props: any) => {
                              if (name === 'amount') {
                                return [
                                  `${formatCurrency(value)} (${props.payload.percentage}%)`,
                                  'Amount',
                                ];
                              }
                              return value;
                            }}
                            cursor={{ fill: 'rgba(99, 102, 241, 0.05)' }}
                          />
                          <Bar
                            dataKey="amount"
                            fill="#6366f1"
                            radius={[0, 6, 6, 0]}
                            isAnimationActive={true}
                            animationDuration={600}
                          >
                            {chartData.map((entry: any, index: number) => (
                              <Cell key={`cell-${index}`} fill={entry.fill} />
                            ))}
                          </Bar>
                        </BarChart>
                      </ResponsiveContainer>
                      <div className={styles['chart-legend']}>
                        {group.categorySpending.map((cat: any) => (
                          <div key={cat.category} className={styles['legend-item']}>
                            <span className={styles['legend-icon']}>
                              {getCategoryIcon(cat.category)}
                            </span>
                            <span className={styles['legend-text']}>
                              {cat.count} transaction{cat.count > 1 ? 's' : ''}
                            </span>
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                );
              })}
              <Button
                variant="ghost"
                fullWidth
                onClick={() => navigate(`/${bookId}/expenses`)}
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
                <Button onClick={() => setShowImportCSVModal(true)}>Add First Expense</Button>
              }
            />
          )}
        </CardContent>
      </Card>

      {/* Modals */}
      <ImportCSVModal
        isOpen={showImportCSVModal}
        onClose={() => setShowImportCSVModal(false)}
        onSuccess={() => {
          setShowImportCSVModal(false);
          showSuccess('Expenses imported successfully!');
          refetch(); // Refresh dashboard after importing
          fetchGroupedTransactions(); // Refresh recent transactions
        }}
      />

      {/* Payment Confirmation Modal */}
      {selectedPayment && (
        <PaymentConfirmationModal
          isOpen={isPaymentModalOpen}
          onClose={() => {
            setIsPaymentModalOpen(false);
            setSelectedPayment(null);
          }}
          onConfirm={handleConfirmPayment}
          loading={isPaymentModalLoading}
          amount={selectedPayment.amount}
          description={selectedPayment.description || selectedPayment.category}
          category={selectedPayment.category}
          frequency={selectedPayment.frequency}
        />
      )}

      {/* Toast Container */}
      <ToastContainer 
        toasts={toasts.map(t => ({
          id: t.id,
          message: t.message,
          type: t.type,
        }))}
        onDismiss={dismissToast}
      />

      <DateRangePicker
        startDate={startDate}
        endDate={endDate}
        onDateRangeChange={handleDateRangeChange}
        isOpen={showDateRangePicker}
        onClose={() => setShowDateRangePicker(false)}
        anchorEl={filterButtonRef.current}
      />
    </div>
  );
};
