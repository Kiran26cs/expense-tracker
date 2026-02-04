import { useEffect } from 'react';
import styles from './Toast.module.css';

export interface ToastProps {
  id: string;
  message: string;
  type?: 'success' | 'error' | 'info' | 'warning';
  onDismiss: (id: string) => void;
  duration?: number;
}

const Toast: React.FC<ToastProps> = ({ 
  id, 
  message, 
  type = 'success', 
  onDismiss,
  duration = 5000 
}) => {
  useEffect(() => {
    const timer = setTimeout(() => {
      onDismiss(id);
    }, duration);

    return () => clearTimeout(timer);
  }, [id, duration, onDismiss]);

  const getIcon = () => {
    switch (type) {
      case 'success':
        return '✓';
      case 'error':
        return '✕';
      case 'warning':
        return '⚠';
      case 'info':
        return 'ℹ';
      default:
        return '✓';
    }
  };

  return (
    <div className={`${styles.toast} ${styles[type]}`}>
      <span className={styles.icon}>{getIcon()}</span>
      <span className={styles.message}>{message}</span>
      <button 
        className={styles.close}
        onClick={() => onDismiss(id)}
        aria-label="Close notification"
      >
        ×
      </button>
    </div>
  );
};

export default Toast;
