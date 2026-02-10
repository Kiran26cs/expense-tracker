import { useState } from 'react';
import { Modal } from '@/components/Modal/Modal';
import { Button } from '@/components/Button/Button';
import { Input } from '@/components/Input/Input';
import styles from './PaymentConfirmationModal.module.css';

interface PaymentConfirmationModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (paidDate: string, recordAsExpense: boolean) => void;
  loading?: boolean;
  amount: number;
  description: string;
  category?: string;
  frequency?: string;
}

export const PaymentConfirmationModal = ({
  isOpen,
  onClose,
  onConfirm,
  loading = false,
  amount,
  description,
  category,
  frequency,
}: PaymentConfirmationModalProps) => {
  const [paidDate, setPaidDate] = useState(new Date().toISOString().split('T')[0]);
  const [recordAsExpense, setRecordAsExpense] = useState(true);

  const handleConfirm = () => {
    if (paidDate) {
      onConfirm(paidDate, recordAsExpense);
    }
  };

  const handleClose = () => {
    setPaidDate(new Date().toISOString().split('T')[0]);
    setRecordAsExpense(true);
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose} size="small" title="Confirm Payment">
      <div className={styles.container}>
        <div className={styles.details}>
          <div className={styles.field}>
            <label className={styles.label}>Description</label>
            <div className={styles.value}>{description}</div>
          </div>

          {category && (
            <div className={styles.field}>
              <label className={styles.label}>Category</label>
              <div className={styles.value} style={{ textTransform: 'capitalize' }}>{category}</div>
            </div>
          )}

          {frequency && (
            <div className={styles.field}>
              <label className={styles.label}>Frequency</label>
              <div className={styles.value} style={{ textTransform: 'capitalize' }}>{frequency}</div>
            </div>
          )}

          <div className={styles.field}>
            <label className={styles.label}>Amount</label>
            <div className={styles.amount}>₹{amount.toFixed(2)}</div>
          </div>

          <Input
            label="Payment Date"
            type="date"
            value={paidDate}
            onChange={(e) => setPaidDate(e.target.value)}
            required
          />

          <label className={styles.checkboxContainer}>
            <input
              type="checkbox"
              checked={recordAsExpense}
              onChange={(e) => setRecordAsExpense(e.target.checked)}
              className={styles.checkbox}
            />
            <span className={styles.checkboxLabel}>
              Record this payment as an expense
            </span>
          </label>

          {!recordAsExpense && (
            <div className={styles.warningNote}>
              <i className="fa-solid fa-exclamation-triangle" style={{ marginRight: '0.5rem' }}></i>
              This payment will NOT be recorded in your expense sheet
            </div>
          )}

          {recordAsExpense && (
            <div className={styles.note}>
              <i className="fa-solid fa-info-circle" style={{ marginRight: '0.5rem' }}></i>
              This payment will be recorded as a completed expense
            </div>
          )}
        </div>

        <div className={styles.actions}>
          <Button
            variant="primary"
            onClick={handleConfirm}
            loading={loading}
            fullWidth
          >
            Confirm Payment
          </Button>
          <Button
            variant="ghost"
            onClick={handleClose}
            fullWidth
            disabled={loading}
          >
            Cancel
          </Button>
        </div>
      </div>
    </Modal>
  );
};
