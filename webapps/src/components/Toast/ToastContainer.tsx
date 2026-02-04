import Toast, { ToastProps } from './Toast';
import styles from './Toast.module.css';

export interface ToastContainerProps {
  toasts: Array<Omit<ToastProps, 'onDismiss'>>;
  onDismiss: (id: string) => void;
}

const ToastContainer: React.FC<ToastContainerProps> = ({ toasts, onDismiss }) => {
  return (
    <div className={styles['toast-container']}>
      {toasts.map((toast) => (
        <Toast
          key={toast.id}
          {...toast}
          onDismiss={onDismiss}
        />
      ))}
    </div>
  );
};

export default ToastContainer;
