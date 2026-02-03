import { ReactNode } from 'react';
import { Button } from '../Button/Button';
import styles from './Loading.module.css';

interface LoadingProps {
  size?: 'small' | 'default' | 'large';
  text?: string;
}

export const Loading = ({ size = 'default', text }: LoadingProps) => {
  return (
    <div className={styles['loading-container']}>
      <div>
        <div className={`${styles.spinner} ${size !== 'default' ? styles[size] : ''}`} />
        {text && <div className={styles['loading-text']}>{text}</div>}
      </div>
    </div>
  );
};

interface EmptyStateProps {
  icon?: string;
  title: string;
  description?: string;
  action?: ReactNode;
}

export const EmptyState = ({ icon = '📭', title, description, action }: EmptyStateProps) => {
  return (
    <div className={styles['empty-state']}>
      <div className={styles['empty-icon']}>{icon}</div>
      <h3 className={styles['empty-title']}>{title}</h3>
      {description && <p className={styles['empty-description']}>{description}</p>}
      {action}
    </div>
  );
};

interface ErrorStateProps {
  title?: string;
  description: string;
  onRetry?: () => void;
}

export const ErrorState = ({
  title = 'Something went wrong',
  description,
  onRetry,
}: ErrorStateProps) => {
  return (
    <div className={styles['error-state']}>
      <div className={styles['error-icon']}>⚠️</div>
      <h3 className={styles['error-title']}>{title}</h3>
      <p className={styles['error-description']}>{description}</p>
      {onRetry && (
        <Button onClick={onRetry} variant="danger">
          Try Again
        </Button>
      )}
    </div>
  );
};
