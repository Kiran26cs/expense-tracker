import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Card } from '@/components/Card/Card';
import { Button } from '@/components/Button/Button';
import { Input } from '@/components/Input/Input';
import { Select } from '@/components/Select/Select';
import { Textarea } from '@/components/Input/Input';
import ToastContainer from '@/components/Toast/ToastContainer';
import { expenseApi } from '@/services/expense.api';
import { useToast } from '@/hooks/useToast';
import { Loading, ErrorState } from '@/components/Loading/Loading';

export const EditExpensePage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { toasts, dismissToast, success: showSuccess, error: showError } = useToast();

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [fetchError, setFetchError] = useState('');

  const [amount, setAmount] = useState('');
  const [date, setDate] = useState('');
  const [category, setCategory] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('');
  const [description, setDescription] = useState('');
  const [notes, setNotes] = useState('');

  // Fetch expense on mount
  useEffect(() => {
    const fetchExpense = async () => {
      if (!id) {
        setFetchError('Expense ID not found');
        setLoading(false);
        return;
      }

      try {
        const response = await expenseApi.getExpense(id);
        if (response.success && response.data) {
          const exp = response.data;
          setAmount(exp.amount.toString());
          setDate(new Date(exp.date).toISOString().split('T')[0]);
          setCategory(typeof exp.category === 'object' ? exp.category.name : exp.category);
          setPaymentMethod(typeof exp.paymentMethod === 'object' ? exp.paymentMethod.type : exp.paymentMethod);
          setDescription(exp.description);
          setNotes(exp.notes || '');
        } else {
          setFetchError(response.error || 'Failed to load expense');
        }
      } catch (err) {
        setFetchError(err instanceof Error ? err.message : 'Failed to load expense');
      } finally {
        setLoading(false);
      }
    };

    fetchExpense();
  }, [id]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError('');

    try {
      const expenseData: any = {
        amount: parseFloat(amount),
        date: new Date(date).toISOString(),
        category,
        paymentMethod,
        description,
        notes: notes || undefined,
      };

      const response = await expenseApi.updateExpense(id!, expenseData);

      if (response.success) {
        showSuccess('Expense updated successfully!');
        setTimeout(() => navigate('/expenses'), 1500);
      } else {
        showError(response.error || 'Failed to update expense');
      }
    } catch (err) {
      showError(err instanceof Error ? err.message : 'Failed to update expense');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <Loading text="Loading expense..." />;
  }

  if (fetchError) {
    return <ErrorState description={fetchError} onRetry={() => navigate('/expenses')} />;
  }

  return (
    <div style={{ maxWidth: '800px', margin: '0 auto' }}>
      <h1 style={{ fontSize: '2rem', fontWeight: 'bold', marginBottom: '2rem' }}>Edit Expense</h1>

      {error && (
        <div
          style={{
            padding: '1rem',
            marginBottom: '1rem',
            backgroundColor: '#fee',
            border: '1px solid #fcc',
            borderRadius: '4px',
            color: '#c33',
          }}
        >
          {error}
        </div>
      )}

      <Card>
        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
          <Input
            label="Amount"
            type="number"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            placeholder="0.00"
            required
            inputSize="large"
            icon="₹"
            step="0.01"
            min="0"
          />

          <Input
            label="Date"
            type="date"
            value={date}
            onChange={(e) => setDate(e.target.value)}
            required
          />

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

          <Select
            label="Payment Method"
            options={[
              { value: 'cash', label: 'Cash' },
              { value: 'card', label: 'Credit/Debit Card' },
              { value: 'upi', label: 'UPI' },
              { value: 'bank', label: 'Bank Transfer' },
            ]}
            value={paymentMethod}
            onChange={(e) => setPaymentMethod(e.target.value)}
            required
          />

          <Input
            label="Description"
            type="text"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Brief description of the expense"
            required
          />

          <Textarea
            label="Notes"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Additional notes (optional)"
            rows={3}
          />

          <div style={{ display: 'flex', gap: '1rem', paddingTop: '1rem' }}>
            <Button type="submit" fullWidth loading={saving}>
              Update Expense
            </Button>
            <Button type="button" variant="ghost" fullWidth onClick={() => navigate('/expenses')}>
              Cancel
            </Button>
          </div>
        </form>
      </Card>

      {/* Toast Container */}
      <ToastContainer 
        toasts={toasts.map(t => ({
          id: t.id,
          message: t.message,
          type: t.type,
        }))}
        onDismiss={dismissToast}
      />
    </div>
  );
};
