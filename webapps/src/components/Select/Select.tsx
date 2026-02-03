import { SelectHTMLAttributes } from 'react';
import styles from './Select.module.css';

interface SelectOption {
  value: string;
  label: string;
}

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  options: SelectOption[];
  error?: string;
  hint?: string;
  required?: boolean;
  placeholder?: string;
}

export const Select = ({
  label,
  options,
  error,
  hint,
  required,
  placeholder = 'Select an option',
  className = '',
  ...props
}: SelectProps) => {
  return (
    <div className={styles['select-wrapper']}>
      {label && (
        <label className={`${styles['select-label']} ${required ? styles.required : ''}`}>
          {label}
        </label>
      )}
      <div className={styles['select-container']}>
        <select className={`${styles.select} ${error ? styles.error : ''} ${className}`} {...props}>
          <option value="">{placeholder}</option>
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
        <span className={styles['select-icon']}>▼</span>
      </div>
      {error && <span className={styles['select-error']}>{error}</span>}
      {hint && !error && <span className={styles['select-hint']}>{hint}</span>}
    </div>
  );
};
