import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Card } from '@/components/Card/Card';
import { Button } from '@/components/Button/Button';
import { Input } from '@/components/Input/Input';
import { Select } from '@/components/Select/Select';
import { Textarea } from '@/components/Input/Input';
import { expenseApi } from '@/services/expense.api';

export const AddExpensePage = () => {
  const [amount, setAmount] = useState('');
  const [date, setDate] = useState(new Date().toISOString().split('T')[0]);
  const [category, setCategory] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('');
  const [description, setDescription] = useState('');
  const [notes, setNotes] = useState('');
  const [isRecurring, setIsRecurring] = useState(false);
  const [frequency, setFrequency] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    
    try {
      const expenseData: any = {
        amount: parseFloat(amount),
        date: new Date(date).toISOString(),
        category,
        paymentMethod,
        description,
        notes: notes || undefined,
        isRecurring,
      };

      if (isRecurring && frequency) {
        expenseData.recurringConfig = {
          frequency,
          startDate: new Date(date).toISOString(),
          endDate: null,
        };
      }

      const response = await expenseApi.createExpense(expenseData);
      
      if (response.success) {
        navigate('/expenses');
      } else {
        setError(response.error || 'Failed to create expense');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create expense');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ maxWidth: '800px', margin: '0 auto' }}>
      <h1 style={{ fontSize: '2rem', fontWeight: 'bold', marginBottom: '2rem' }}>Add Expense</h1>

      {error && (
        <div style={{ 
          padding: '1rem', 
          marginBottom: '1rem', 
          backgroundColor: '#fee', 
          border: '1px solid #fcc',
          borderRadius: '4px',
          color: '#c33'
        }}>
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

          <div>
            <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer' }}>
              <input
                type="checkbox"
                checked={isRecurring}
                onChange={(e) => setIsRecurring(e.target.checked)}
              />
              <span>This is a recurring expense</span>
            </label>
          </div>

          {isRecurring && (
            <Select
              label="Frequency"
              options={[
                { value: 'daily', label: 'Daily' },
                { value: 'weekly', label: 'Weekly' },
                { value: 'monthly', label: 'Monthly' },
                { value: 'yearly', label: 'Yearly' },
              ]}
              value={frequency}
              onChange={(e) => setFrequency(e.target.value)}
              required={isRecurring}
            />
          )}

          <div style={{ display: 'flex', gap: '1rem', paddingTop: '1rem' }}>
            <Button type="submit" fullWidth loading={loading}>
              Save Expense
            </Button>
            <Button type="button" variant="ghost" fullWidth onClick={() => navigate('/expenses')}>
              Cancel
            </Button>
          </div>
        </form>
      </Card>
    </div>
  );
};
