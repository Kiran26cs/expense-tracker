import { useState, useRef, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/Card/Card';
import { Button } from '@/components/Button/Button';
import { EmptyState } from '@/components/Loading/Loading';
import { RecurringExpensesList } from '@/components/RecurringExpensesList';
import { AddRecurringModal } from '@/components/AddRecurringModal';
import { DateRangePicker } from '@/components/DateRangePicker/DateRangePicker';
import { useToast } from '@/hooks/useToast';

// Helper to get date string for X days from now
const getDateFromNow = (days: number) => {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return date.toISOString().split('T')[0];
};

// Format date as "Feb 23"
const formatShortDate = (dateStr: string) => {
  const date = new Date(dateStr);
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
};

export const InsightsPage = () => {
  const navigate = useNavigate();
  const { bookId } = useParams<{ bookId: string }>();
  const { toasts, dismissToast, success, error } = useToast();
  const [showDatePicker, setShowDatePicker] = useState(false);
  const [isAddRecurringOpen, setIsAddRecurringOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [startDate, setStartDate] = useState(new Date().toISOString().split('T')[0]);
  const [endDate, setEndDate] = useState(getDateFromNow(7));
  const dateFilterButtonRef = useRef<HTMLButtonElement>(null);

  // Redirect if no bookId
  useEffect(() => {
    if (!bookId) {
      navigate('/');
    }
  }, [bookId, navigate]);

  const handleDateRangeChange = (start: string, end: string) => {
    setStartDate(start);
    setEndDate(end);
    setRefreshKey(k => k + 1);
  };

  const clearDateFilter = () => {
    setStartDate('');
    setEndDate('');
    setRefreshKey(k => k + 1);
  };

  const hasDateFilter = startDate || endDate;

  return (
    <div>
      <h1 style={{ fontSize: '2rem', fontWeight: 'bold', marginBottom: '2rem' }}>Insights & Forecast</h1>

      <div style={{ display: 'grid', gap: '1.5rem' }}>
        <Card>
          <CardHeader>
            <CardTitle>Cash Runway</CardTitle>
          </CardHeader>
          <CardContent>
            <div style={{ textAlign: 'center', padding: '2rem' }}>
              <div style={{ fontSize: '3rem', fontWeight: 'bold', color: 'var(--color-success)' }}>45 days</div>
              <p style={{ color: 'var(--color-text-secondary)', marginTop: '0.5rem' }}>
                Based on your current spending patterns
              </p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Cash Flow Forecast</CardTitle>
          </CardHeader>
          <CardContent>
            <EmptyState
              icon="📈"
              title="Forecast unavailable"
              description="Add more expenses to generate accurate forecasts"
            />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>"Can I Buy This?" Simulator</CardTitle>
          </CardHeader>
          <CardContent>
            <p style={{ color: 'var(--color-text-secondary)', marginBottom: '1rem' }}>
              Check if a purchase will impact your budget and savings goals
            </p>
            <Button>Open Simulator</Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <CardTitle>Recurring Expenses</CardTitle>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
              {/* Date Range Badge */}
              {hasDateFilter && (
                <div style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '0.5rem',
                  padding: '0.5rem 1rem',
                  background: '#6366f1',
                  borderRadius: '9999px',
                  color: 'white',
                  fontSize: '0.875rem',
                  fontWeight: 500,
                }}>
                  <span>
                    {startDate && endDate 
                      ? `${formatShortDate(startDate)} - ${formatShortDate(endDate)}`
                      : startDate 
                        ? `From ${formatShortDate(startDate)}`
                        : `To ${formatShortDate(endDate)}`
                    }
                  </span>
                  <button
                    onClick={clearDateFilter}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      background: 'transparent',
                      border: 'none',
                      color: 'white',
                      cursor: 'pointer',
                      padding: '2px',
                      marginLeft: '0.25rem',
                    }}
                  >
                    <i className="fa-solid fa-times" style={{ fontSize: '0.75rem' }}></i>
                  </button>
                </div>
              )}
              {/* Calendar Icon Button */}
              <button
                ref={dateFilterButtonRef}
                onClick={() => setShowDatePicker(!showDatePicker)}
                title="Filter by date range"
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  width: '40px',
                  height: '40px',
                  borderRadius: '8px',
                  border: '2px solid var(--border-color)',
                  background: 'var(--color-background)',
                  color: 'var(--color-text-secondary)',
                  cursor: 'pointer',
                  transition: 'all 0.2s ease',
                  fontSize: '1.1rem',
                }}
              >
                <i className="fa-regular fa-calendar"></i>
              </button>
              <Button variant="primary" onClick={() => setIsAddRecurringOpen(true)}>
                <i className="fa-solid fa-plus" style={{ marginRight: '0.5rem' }}></i>
                Add Recurring
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            <RecurringExpensesList 
              key={refreshKey}
              expenseBookId={bookId || undefined}
              startDate={startDate}
              endDate={endDate}
              onPaymentSuccess={() => setRefreshKey(k => k + 1)}
              onShowSuccess={success}
              onShowError={error}
            />
          </CardContent>
        </Card>

        <DateRangePicker
          startDate={startDate}
          endDate={endDate}
          onDateRangeChange={handleDateRangeChange}
          isOpen={showDatePicker}
          onClose={() => setShowDatePicker(false)}
          anchorEl={dateFilterButtonRef.current}
        />
      </div>

      {/* Add Recurring Expense Modal */}
      <AddRecurringModal
        isOpen={isAddRecurringOpen}
        onClose={() => setIsAddRecurringOpen(false)}
        expenseBookId={bookId || ''}
        onSuccess={() => {
          setIsAddRecurringOpen(false);
          success('Recurring expense added successfully!');
          setRefreshKey(k => k + 1);
        }}
      />
      {/* Toast Container */}
      <div style={{ position: 'fixed', top: '1rem', right: '1rem', zIndex: 99999 }}>
        {toasts.map((toast) => (
          <div
            key={toast.id}
            style={{
              marginBottom: '0.75rem',
              padding: '1rem 1.25rem',
              borderRadius: '0.5rem',
              backgroundColor: toast.type === 'success' ? '#10b981' : '#ef4444',
              color: 'white',
              minWidth: '320px',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              boxShadow: '0 10px 25px rgba(0, 0, 0, 0.2)',
              fontSize: '0.95rem',
              fontWeight: 500,
            }}
          >
            <span>{toast.message}</span>
            <button
              onClick={() => dismissToast(toast.id)}
              style={{
                background: 'none',
                border: 'none',
                color: 'white',
                cursor: 'pointer',
                padding: '0 0 0 1rem',
                fontSize: '1.2rem',
              }}
            >
              ✕
            </button>
          </div>
        ))}
      </div>    </div>
  );
};
