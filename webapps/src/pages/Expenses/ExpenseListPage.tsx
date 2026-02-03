import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/Card/Card';
import { Button } from '@/components/Button/Button';
import { Input } from '@/components/Input/Input';
import { Select } from '@/components/Select/Select';
import { EmptyState, Loading, ErrorState } from '@/components/Loading/Loading';
import { expenseApi } from '@/services/expense.api';
import { formatCurrency, formatDate } from '@/utils/helpers';
import { ActionMenu } from '@/components/ActionMenu/ActionMenu';
import { ImportCSVModal } from '@/components/ImportCSV/ImportCSVModal';
import { AddExpenseModal } from '@/components/AddExpenseModal/AddExpenseModal';
import type { Expense } from '@/types';

export const ExpenseListPage = () => {
  const navigate = useNavigate();
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [importModalOpen, setImportModalOpen] = useState(false);
  const [addExpenseModalOpen, setAddExpenseModalOpen] = useState(false);
  
  // Filter states
  const [searchQuery, setSearchQuery] = useState('');
  const [category, setCategory] = useState('all');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');

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

  const fetchExpenses = async () => {
    setLoading(true);
    setError('');
    try {
      const filters: any = {};
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

  useEffect(() => {
    fetchExpenses();
  }, [startDate, endDate, category]);

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

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
        <h1 style={{ fontSize: '2rem', fontWeight: 'bold' }}>Expenses</h1>
        <ActionMenu 
          onAddExpense={() => setAddExpenseModalOpen(true)}
          onImportCSV={() => setImportModalOpen(true)}
        />
      </div>

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
          <Input 
            type="date" 
            placeholder="Start date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
          />
          <Input 
            type="date" 
            placeholder="End date"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
          />
        </div>
      </Card>

      {filteredExpenses.length === 0 ? (
        <Card>
          <EmptyState
            icon="💸"
            title="No expenses found"
            description={searchQuery || startDate || endDate || category !== 'all' 
              ? "No expenses match your filters. Try adjusting your search criteria."
              : "Start tracking by adding your first expense"}
            action={<Button onClick={() => navigate('/expenses/add')}>Add Expense</Button>}
          />
        </Card>
      ) : (
        <Card>
          <CardHeader>
            <CardTitle>All Expenses ({filteredExpenses.length})</CardTitle>
          </CardHeader>
          <CardContent>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
              {filteredExpenses.map((expense) => (
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
                    onClick={() => navigate(`/expenses/${expense.id}/edit`)}
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

      <AddExpenseModal
        isOpen={addExpenseModalOpen}
        onClose={() => setAddExpenseModalOpen(false)}
        onSuccess={() => {
          setAddExpenseModalOpen(false);
          fetchExpenses(); // Refresh expense list
        }}
      />
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
