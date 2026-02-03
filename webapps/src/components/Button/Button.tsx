import { ReactNode, ButtonHTMLAttributes } from 'react';
import styles from './Button.module.css';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  children: ReactNode;
  variant?: 'primary' | 'secondary' | 'success' | 'warning' | 'danger' | 'ghost';
  size?: 'small' | 'default' | 'large';
  fullWidth?: boolean;
  loading?: boolean;
  iconOnly?: boolean;
  className?: string;
}

export const Button = ({
  children,
  variant = 'primary',
  size = 'default',
  fullWidth = false,
  loading = false,
  iconOnly = false,
  className = '',
  disabled,
  ...props
}: ButtonProps) => {
  const classes = [
    styles.button,
    styles[variant],
    size !== 'default' && styles[size],
    fullWidth && styles['full-width'],
    iconOnly && styles['icon-only'],
    loading && styles.loading,
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <button className={classes} disabled={disabled || loading} {...props}>
      {children}
    </button>
  );
};
