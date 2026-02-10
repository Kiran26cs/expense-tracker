import { useState } from 'react';
import { Input } from '@/components/Input/Input';
import { Select } from '@/components/Select/Select';
import { Button } from '@/components/Button/Button';
import { expenseApi } from '@/services/expense.api';
import styles from './AddRecurringModal.module.css';

interface AddRecurringModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  expenseBookId: string;
}

export const AddRecurringModal = ({ isOpen, onClose, onSuccess, expenseBookId }: AddRecurringModalProps) => {
  const [amount, setAmount] = useState('');
  const [category, setCategory] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('');
  const [description, setDescription] = useState('');
  const [frequency, setFrequency] = useState('monthly');
  const [startDate, setStartDate] = useState(new Date().toISOString().split('T')[0]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const resetForm = () => {
    setAmount('');
    setCategory('');
    setPaymentMethod('');
    setDescription('');
    setFrequency('monthly');
    setStartDate(new Date().toISOString().split('T')[0]);
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
      const expenseData = {
        amount: parseFloat(amount),
        date: new Date(startDate).toISOString(),
        category,
        paymentMethod,
        description,
        isRecurring: true,
        expenseBookId: expenseBookId,
        recurringConfig: {
          frequency,
          startDate: new Date(startDate).toISOString(),
          endDate: null,
        },
      };

      const response = await expenseApi.createExpense(expenseData);

      if (response.success) {
        resetForm();
        onSuccess();
        onClose();
      } else {
        setError(response.error || 'Failed to create recurring expense');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create recurring expense');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className={styles.overlay} onClick={handleClose}>
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        <div className={styles.header}>
          <h2 className={styles.title}>Add Recurring Expense</h2>
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
              icon="₹"
            />

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
                { value: 'subscriptions', label: '🎯 Subscriptions' },
                { value: 'healthcare', label: '🏥 Healthcare' },
                { value: 'education', label: '📚 Education' },
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
            placeholder="e.g. Netflix Subscription, Internet Bill"
            required
          />

          <Input
            label="Start Date"
            type="date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
            required
          />

          <div className={styles.info}>
            <i className="fa-solid fa-info-circle" style={{ marginRight: '0.5rem' }}></i>
            This will create a recurring expense and auto-generate upcoming payment entries for the next 30 days.
          </div>

          <div className={styles.actions}>
            <Button type="button" variant="ghost" onClick={handleClose} disabled={loading}>
              Cancel
            </Button>
            <Button type="submit" disabled={loading}>
              {loading ? 'Saving...' : 'Add Recurring Expense'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
