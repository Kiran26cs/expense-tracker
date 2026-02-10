import { useState, useEffect, useRef } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { BarChart, Bar, XAxis, YAxis, ResponsiveContainer, Tooltip, Cell } from 'recharts';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/Card/Card';
import { Button } from '@/components/Button/Button';
import { Input } from '@/components/Input/Input';
import { Select } from '@/components/Select/Select';
import { EmptyState, Loading, ErrorState } from '@/components/Loading/Loading';
import { ImportCSVModal } from '@/components/ImportCSV/ImportCSVModal';
import { DateRangePicker } from '@/components/DateRangePicker/DateRangePicker';
import { expenseApi } from '@/services/expense.api';
import { dashboardApi } from '@/services/dashboard.api';
import { formatCurrency, formatDate } from '@/utils/helpers';
import type { Expense } from '@/types';
import styles from './ExpenseList.module.css';

export const ExpenseListPage = () => {
  const navigate = useNavigate();
  const { bookId } = useParams<{ bookId: string }>();
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [importModalOpen, setImportModalOpen] = useState(false);
  const [activeTab, setActiveTab] = useState<'expenses' | 'transactions'>('expenses');
  
  // Filter states
  const [searchQuery, setSearchQuery] = useState('');
  const [category, setCategory] = useState('all');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [showDateRangePicker, setShowDateRangePicker] = useState(false);
  const [showScrollTop, setShowScrollTop] = useState(false);
  const dateFilterButtonRef = useRef<HTMLButtonElement>(null);

  const handleDateRangeChange = (start: string, end: string) => {
    setStartDate(start);
    setEndDate(end);
  };

  // Handle scroll to show/hide scroll-to-top button
  useEffect(() => {
    const handleScroll = () => {
      // Show button after scrolling 800px (approximately 20 records at ~40px each)
      setShowScrollTop(window.scrollY > 800);
    };

    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  const scrollToTop = () => {
    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });
  };

  // Pagination states
  const [currentPage, setCurrentPage] = useState(1);
  const [itemsPerPage] = useState(50);
  
  // Grouped transactions state
  const [groupedTransactions, setGroupedTransactions] = useState<any[]>([]);
  const [loadingGrouped, setLoadingGrouped] = useState(false);
  const [transactionsPage, setTransactionsPage] = useState(1);
  const [transactionsPerPage] = useState(10);

  // Redirect if no bookId
  useEffect(() => {
    if (!bookId) {
      navigate('/');
    }
  }, [bookId, navigate]);

  // Filtered expenses (client-side search filtering)
  const filteredExpenses = expenses.filter(expense => {
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      const matchesDescription = expense.description?.toLowerCase().includes(query);
      const matchesNotes = expense.notes?.toLowerCase().includes(query);
      const matchesAmount = expense.amount.toString().includes(query);
      if (!matchesDescription && !matchesNotes && !matchesAmount) return false;
    }
    return true;
  });

  // Pagination for expenses
  const indexOfLastExpense = currentPage * itemsPerPage;
  const indexOfFirstExpense = indexOfLastExpense - itemsPerPage;
  const currentExpenses = filteredExpenses.slice(indexOfFirstExpense, indexOfLastExpense);
  const totalPages = Math.ceil(filteredExpenses.length / itemsPerPage);

  // Pagination for transactions
  const indexOfLastTransaction = transactionsPage * transactionsPerPage;
  const indexOfFirstTransaction = indexOfLastTransaction - transactionsPerPage;
  const currentTransactions = groupedTransactions.slice(indexOfFirstTransaction, indexOfLastTransaction);
  const totalTransactionPages = Math.ceil(groupedTransactions.length / transactionsPerPage);

  const fetchExpenses = async () => {
    if (!bookId) return;
    
    setLoading(true);
    setError('');
    try {
      const filters: any = { expenseBookId: bookId };
      if (startDate) filters.startDate = new Date(startDate).toISOString();
      if (endDate) filters.endDate = new Date(endDate).toISOString();
      if (category && category !== 'all') filters.category = category;

      const response = await expenseApi.getExpenses(filters);
      
      if (response.success && response.data) {
        setExpenses(response.data);
      } else {
        setError(response.error || 'Failed to load expenses');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load expenses');
    } finally {
      setLoading(false);
    }
  };

  const fetchGroupedTransactions = async () => {
    if (!bookId) return;
    
    setLoadingGrouped(true);
    try {
      const response = await dashboardApi.getGroupedTransactions(bookId, startDate, endDate);
      if (response.success && response.data) {
        setGroupedTransactions(response.data);
      }
    } catch (err) {
      console.error('Failed to load grouped transactions:', err);
    } finally {
      setLoadingGrouped(false);
    }
  };

  useEffect(() => {
    if (bookId) {
      fetchExpenses();
      if (activeTab === 'transactions') {
        fetchGroupedTransactions();
      }
    }
  }, [startDate, endDate, category, activeTab, bookId]);

  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery, category, startDate, endDate]);

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this expense?')) return;
    
    try {
      const response = await expenseApi.deleteExpense(id);
      if (response.success) {
        setExpenses(expenses.filter(e => e.id !== id));
      } else {
        alert(response.error || 'Failed to delete expense');
      }
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to delete expense');
    }
  };

  if (loading) {
    return <Loading text="Loading expenses..." />;
  }

  if (error) {
    return <ErrorState description={error} onRetry={fetchExpenses} />;
  }

  const getCategoryColor = (category: string) => {
    const colors: Record<string, string> = {
      food: '#f59e0b',
      transport: '#6366f1',
      shopping: '#ec4899',
      bills: '#10b981',
      entertainment: '#8b5cf6',
    };
    return colors[category] || '#6366f1';
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
        <h1 style={{ fontSize: '2rem', fontWeight: 'bold' }}>Expenses</h1>
        <Button variant="primary" onClick={() => setImportModalOpen(true)}>
          <i className="fa-solid fa-plus" style={{ marginRight: '0.5rem' }}></i>
          Add Expense
        </Button>
      </div>

      {/* Tabs */}
      <div className={styles.tabs}>
        <button 
          className={`${styles.tab} ${activeTab === 'expenses' ? styles.active : ''}`}
          onClick={() => setActiveTab('expenses')}
        >
          All Expenses
        </button>
        <button 
          className={`${styles.tab} ${activeTab === 'transactions' ? styles.active : ''}`}
          onClick={() => setActiveTab('transactions')}
        >
          Daily Transactions
        </button>
      </div>

      {/* Filters - Only for All Expenses tab */}
      {activeTab === 'expenses' && (
        <Card style={{ marginBottom: '1.5rem' }}>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', gap: '1rem' }}>
            <Input
              placeholder="Search transactions..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
            <Select
              options={[
                { value: 'all', label: 'All Categories' },
                { value: 'food', label: '🍔 Food & Dining' },
                { value: 'transport', label: '🚗 Transportation' },
                { value: 'shopping', label: '🛍️ Shopping' },
                { value: 'bills', label: '📱 Bills & Utilities' },
                { value: 'entertainment', label: '🎬 Entertainment' },
              ]}
              value={category}
              onChange={(e) => setCategory(e.target.value)}
              placeholder="Filter by category"
            />
            <div className={styles.filterSection}>
              {startDate && endDate && (
                <div className={styles.dateChip}>
                  {new Date(startDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} - {new Date(endDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                  <button 
                    className={styles.chipClose}
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
                ref={dateFilterButtonRef}
                className={styles.filterButton}
                onClick={() => setShowDateRangePicker(!showDateRangePicker)}
                aria-label="Filter by date range"
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
        </Card>
      )}

      {/* Tab Content */}
      {activeTab === 'expenses' ? (
        <>
          {filteredExpenses.length === 0 ? (
            <Card>
              <EmptyState
                icon="💸"
                title="No expenses found"
                description={searchQuery || startDate || endDate || category !== 'all' 
                  ? "No expenses match your filters. Try adjusting your search criteria."
                  : "Start tracking by adding your first expense"}
                action={<Button onClick={() => navigate(`/${bookId}/expenses/add`)}>Add Expense</Button>}
              />
            </Card>
          ) : (
            <>
              <Card>
                <CardHeader>
                  <CardTitle>All Expenses ({filteredExpenses.length})</CardTitle>
                </CardHeader>
                <CardContent>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                    {currentExpenses.map((expense) => (
                      <div
                        key={expense.id}
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          padding: '1rem',
                          borderBottom: '1px solid var(--border-color)',
                          gap: '1rem',
                        }}
                      >
                        <div
                          style={{
                            width: '40px',
                            height: '40px',
                            borderRadius: '8px',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            fontSize: '1.5rem',
                            flexShrink: 0,
                            backgroundColor: 'rgba(99, 102, 241, 0.1)',
                          }}
                        >
                          {getCategoryIcon(expense.category)}
                        </div>
                        <div style={{ flex: 1, minWidth: 0 }}>
                          <div style={{ fontWeight: 600, fontSize: '0.95rem', marginBottom: '0.25rem' }}>
                            {expense.description || 'No description'}
                          </div>
                          <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
                            {expense.category} • {formatDate(expense.date, 'short')} • {expense.paymentMethod}
                            {expense.notes && ` • ${expense.notes}`}
                          </div>
                        </div>
                        <div style={{ fontWeight: 700, fontSize: '1.1rem', flexShrink: 0 }}>
                          {formatCurrency(expense.amount)}
                        </div>
                        <Button
                          variant="ghost"
                          onClick={() => navigate(`/${bookId}/expenses/${expense.id}/edit`)}
                          style={{ flexShrink: 0 }}
                          title="Edit expense"
                        >
                          ✏️
                        </Button>
                        <Button
                          variant="ghost"
                          onClick={() => handleDelete(expense.id)}
                          style={{ flexShrink: 0 }}
                          title="Delete expense"
                        >
                          🗑️
                        </Button>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>

              {/* Pagination for Expenses */}
              {totalPages > 1 && (
                <div className={styles.pagination}>
                  <button 
                    className={styles.pageButton}
                    onClick={() => setCurrentPage(1)}
                    disabled={currentPage === 1}
                  >
                    First
                  </button>
                  <button 
                    className={styles.pageButton}
                    onClick={() => setCurrentPage(currentPage - 1)}
                    disabled={currentPage === 1}
                  >
                    Previous
                  </button>
                  <span className={styles.pageInfo}>
                    Page {currentPage} of {totalPages}
                  </span>
                  <button 
                    className={styles.pageButton}
                    onClick={() => setCurrentPage(currentPage + 1)}
                    disabled={currentPage === totalPages}
                  >
                    Next
                  </button>
                  <button 
                    className={styles.pageButton}
                    onClick={() => setCurrentPage(totalPages)}
                    disabled={currentPage === totalPages}
                  >
                    Last
                  </button>
                </div>
              )}
            </>
          )}
        </>
      ) : (
        <>
          {/* Daily Transactions Tab */}
          {loadingGrouped ? (
            <Card>
              <Loading text="Loading transactions..." />
            </Card>
          ) : currentTransactions.length === 0 ? (
            <Card>
              <EmptyState
                icon="📊"
                title="No transactions found"
                description="No daily transactions available for the selected period"
              />
            </Card>
          ) : (
            <>
              <div className={styles.sectionHeader}>
                <h2 className={styles.sectionTitle}>Transaction Summary</h2>
                <div className={styles.filterSection}>
                  {startDate && endDate && (
                    <div className={styles.dateChip}>
                      {new Date(startDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} - {new Date(endDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                      <button 
                        className={styles.chipClose}
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
                    ref={dateFilterButtonRef}
                    className={styles.filterButton}
                    onClick={() => setShowDateRangePicker(!showDateRangePicker)}
                    aria-label="Filter by date range"
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
              <Card>
                <CardContent>
                  {currentTransactions.map((group: any) => {
                    const chartData = group.categorySpending.map((cat: any) => ({
                      name: cat.category,
                      amount: cat.amount,
                      percentage: ((cat.amount / group.totalSpent) * 100).toFixed(1),
                      count: cat.count,
                      fill: getCategoryColor(cat.category),
                    }));

                    return (
                      <div key={group.date} className={styles.dateGroup}>
                        <div className={styles.dateHeader}>
                          <span className={styles.dateLabel}>{group.dateLabel}</span>
                          <span className={styles.dateTotal}>{formatCurrency(group.totalSpent)}</span>
                        </div>
                        <div className={styles.chartWrapper}>
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
                        </div>
                      </div>
                    );
                  })}
                </CardContent>
              </Card>

              {/* Pagination for Transactions */}
              {totalTransactionPages > 1 && (
                <div className={styles.pagination}>
                  <button 
                    className={styles.pageButton}
                    onClick={() => setTransactionsPage(1)}
                    disabled={transactionsPage === 1}
                  >
                    First
                  </button>
                  <button 
                    className={styles.pageButton}
                    onClick={() => setTransactionsPage(transactionsPage - 1)}
                    disabled={transactionsPage === 1}
                  >
                    Previous
                  </button>
                  <span className={styles.pageInfo}>
                    Page {transactionsPage} of {totalTransactionPages}
                  </span>
                  <button 
                    className={styles.pageButton}
                    onClick={() => setTransactionsPage(transactionsPage + 1)}
                    disabled={transactionsPage === totalTransactionPages}
                  >
                    Next
                  </button>
                  <button 
                    className={styles.pageButton}
                    onClick={() => setTransactionsPage(totalTransactionPages)}
                    disabled={transactionsPage === totalTransactionPages}
                  >
                    Last
                  </button>
                </div>
              )}
            </>
          )}
        </>
      )}

      <ImportCSVModal 
        isOpen={importModalOpen}
        onClose={() => setImportModalOpen(false)}
        onSuccess={(count) => {
          setImportModalOpen(false);
          // Refresh the expense list
          setTimeout(() => fetchExpenses(), 500);
        }}
      />

      <DateRangePicker
        startDate={startDate}
        endDate={endDate}
        onDateRangeChange={handleDateRangeChange}
        isOpen={showDateRangePicker}
        onClose={() => setShowDateRangePicker(false)}
        anchorEl={dateFilterButtonRef.current}
      />

      {/* Scroll to Top Button */}
      {showScrollTop && activeTab === 'expenses' && (
        <button
          className={styles.scrollToTopButton}
          onClick={scrollToTop}
          aria-label="Scroll to top"
          title="Back to top"
        >
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="18 15 12 9 6 15"></polyline>
          </svg>
        </button>
      )}
    </div>
  );
};

// Helper function to get category icon
function getCategoryIcon(category: any): string {
  if (typeof category === 'object' && category.icon) {
    return category.icon;
  }
  
  const categoryIcons: Record<string, string> = {
    food: '🍔',
    transport: '🚗',
    shopping: '🛍️',
    bills: '📱',
    entertainment: '🎬',
  };
  
  return categoryIcons[category] || '💰';
}
