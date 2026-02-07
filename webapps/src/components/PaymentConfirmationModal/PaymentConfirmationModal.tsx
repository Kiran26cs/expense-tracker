import { useState } from 'react';
import { Modal } from '@/components/Modal/Modal';
import { Button } from '@/components/Button/Button';
import { Input } from '@/components/Input/Input';
import styles from './PaymentConfirmationModal.module.css';

interface PaymentConfirmationModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (paidDate: string) => void;
  loading?: boolean;
  amount: number;
  description: string;
}

export const PaymentConfirmationModal = ({
  isOpen,
  onClose,
  onConfirm,
  loading = false,
  amount,
  description,
}: PaymentConfirmationModalProps) => {
  const [paidDate, setPaidDate] = useState(new Date().toISOString().split('T')[0]);

  const handleConfirm = () => {
    if (paidDate) {
      onConfirm(paidDate);
    }
  };

  const handleClose = () => {
    setPaidDate(new Date().toISOString().split('T')[0]);
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

          <div className={styles.note}>
            <i className="fa-solid fa-info-circle" style={{ marginRight: '0.5rem' }}></i>
            This will record the payment as a completed expense
          </div>
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
