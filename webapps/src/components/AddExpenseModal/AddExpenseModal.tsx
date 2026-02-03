import { useState } from 'react';
import { Input, Textarea } from '@/components/Input/Input';
import { Select } from '@/components/Select/Select';
import { Button } from '@/components/Button/Button';
import { expenseApi } from '@/services/expense.api';
import styles from './AddExpenseModal.module.css';

interface AddExpenseModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export const AddExpenseModal = ({ isOpen, onClose, onSuccess }: AddExpenseModalProps) => {
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

  const resetForm = () => {
    setAmount('');
    setDate(new Date().toISOString().split('T')[0]);
    setCategory('');
    setPaymentMethod('');
    setDescription('');
    setNotes('');
    setIsRecurring(false);
    setFrequency('');
    setError('');
  };

  const handleClose = () => {
    resetForm();
    onClose();
  };

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
        resetForm();
        onSuccess();
        onClose();
      } else {
        setError(response.error || 'Failed to create expense');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create expense');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className={styles.overlay} onClick={handleClose}>
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        <div className={styles.header}>
          <h2 className={styles.title}>Add Expense</h2>
          <button className={styles.closeButton} onClick={handleClose}>
            ✕
          </button>
        </div>

        {error && (
          <div className={styles.error}>
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className={styles.form}>
          <div className={styles.row}>
            <Input
              label="Amount"
              type="number"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              placeholder="0.00"
              required
              inputSize="large"
              icon="$"
            />

            <Input
              label="Date"
              type="date"
              value={date}
              onChange={(e) => setDate(e.target.value)}
              required
            />
          </div>

          <div className={styles.row}>
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
          </div>

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

          <div className={styles.checkboxWrapper}>
            <label className={styles.checkbox}>
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

          <div className={styles.actions}>
            <Button type="button" variant="ghost" onClick={handleClose} disabled={loading}>
              Cancel
            </Button>
            <Button type="submit" disabled={loading}>
              {loading ? 'Saving...' : 'Save Expense'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
