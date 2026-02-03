import { InputHTMLAttributes, TextareaHTMLAttributes, ReactNode } from 'react';
import styles from './Input.module.css';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  hint?: string;
  required?: boolean;
  icon?: ReactNode;
  inputSize?: 'small' | 'default' | 'large';
}

export const Input = ({
  label,
  error,
  hint,
  required,
  icon,
  inputSize = 'default',
  className = '',
  ...props
}: InputProps) => {
  const inputClasses = [
    styles.input,
    error && styles.error,
    icon && styles['with-icon'],
    inputSize !== 'default' && styles[inputSize],
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div className={styles['input-wrapper']}>
      {label && (
        <label className={`${styles['input-label']} ${required ? styles.required : ''}`}>
          {label}
        </label>
      )}
      <div className={styles['input-container']}>
        {icon && <span className={styles['input-icon']}>{icon}</span>}
        <input className={inputClasses} {...props} />
      </div>
      {error && <span className={styles['input-error']}>{error}</span>}
      {hint && !error && <span className={styles['input-hint']}>{hint}</span>}
    </div>
  );
};

interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  error?: string;
  hint?: string;
  required?: boolean;
}

export const Textarea = ({
  label,
  error,
  hint,
  required,
  className = '',
  ...props
}: TextareaProps) => {
  const textareaClasses = [styles.input, styles.textarea, error && styles.error, className]
    .filter(Boolean)
    .join(' ');

  return (
    <div className={styles['input-wrapper']}>
      {label && (
        <label className={`${styles['input-label']} ${required ? styles.required : ''}`}>
          {label}
        </label>
      )}
      <textarea className={textareaClasses} {...props} />
      {error && <span className={styles['input-error']}>{error}</span>}
      {hint && !error && <span className={styles['input-hint']}>{hint}</span>}
    </div>
  );
};
