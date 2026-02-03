import { ReactNode, useEffect } from 'react';
import { createPortal } from 'react-dom';
import styles from './Modal.module.css';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  children: ReactNode;
  size?: 'small' | 'default' | 'large' | 'full';
  title?: string;
  footer?: ReactNode;
  closeOnOverlayClick?: boolean;
  closeOnEscape?: boolean;
}

export const Modal = ({
  isOpen,
  onClose,
  children,
  size = 'default',
  title,
  footer,
  closeOnOverlayClick = true,
  closeOnEscape = true,
}: ModalProps) => {
  useEffect(() => {
    if (!isOpen) return;

    const handleEscape = (e: KeyboardEvent) => {
      if (closeOnEscape && e.key === 'Escape') {
        onClose();
      }
    };

    document.addEventListener('keydown', handleEscape);
    document.body.style.overflow = 'hidden';

    return () => {
      document.removeEventListener('keydown', handleEscape);
      document.body.style.overflow = '';
    };
  }, [isOpen, closeOnEscape, onClose]);

  if (!isOpen) return null;

  const handleOverlayClick = (e: React.MouseEvent) => {
    if (closeOnOverlayClick && e.target === e.currentTarget) {
      onClose();
    }
  };

  const modalContent = (
    <div className={styles['modal-overlay']} onClick={handleOverlayClick}>
      <div className={`${styles.modal} ${size !== 'default' ? styles[size] : ''}`}>
        {title && (
          <div className={styles['modal-header']}>
            <h2 className={styles['modal-title']}>{title}</h2>
            <button className={styles['modal-close']} onClick={onClose} aria-label="Close">
              ✕
            </button>
          </div>
        )}
        <div className={styles['modal-content']}>{children}</div>
        {footer && <div className={styles['modal-footer']}>{footer}</div>}
      </div>
    </div>
  );

  return createPortal(modalContent, document.body);
};
