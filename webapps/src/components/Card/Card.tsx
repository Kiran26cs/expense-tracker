import { ReactNode } from 'react';
import styles from './Card.module.css';

interface CardProps {
  children: ReactNode;
  className?: string;
  variant?: 'default' | 'outline' | 'elevated';
  size?: 'default' | 'compact';
  clickable?: boolean;
  onClick?: () => void;
}

export const Card = ({
  children,
  className = '',
  variant = 'default',
  size = 'default',
  clickable = false,
  onClick,
}: CardProps) => {
  const classes = [
    styles.card,
    variant !== 'default' && styles[variant],
    size !== 'default' && styles[size],
    clickable && styles.clickable,
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div className={classes} onClick={onClick}>
      {children}
    </div>
  );
};

interface CardHeaderProps {
  children: ReactNode;
  className?: string;
}

export const CardHeader = ({ children, className = '' }: CardHeaderProps) => {
  return <div className={`${styles['card-header']} ${className}`}>{children}</div>;
};

interface CardTitleProps {
  children: ReactNode;
  className?: string;
}

export const CardTitle = ({ children, className = '' }: CardTitleProps) => {
  return <h3 className={`${styles['card-title']} ${className}`}>{children}</h3>;
};

interface CardSubtitleProps {
  children: ReactNode;
  className?: string;
}

export const CardSubtitle = ({ children, className = '' }: CardSubtitleProps) => {
  return <p className={`${styles['card-subtitle']} ${className}`}>{children}</p>;
};

interface CardContentProps {
  children: ReactNode;
  className?: string;
}

export const CardContent = ({ children, className = '' }: CardContentProps) => {
  return <div className={`${styles['card-content']} ${className}`}>{children}</div>;
};

interface CardFooterProps {
  children: ReactNode;
  className?: string;
}

export const CardFooter = ({ children, className = '' }: CardFooterProps) => {
  return <div className={`${styles['card-footer']} ${className}`}>{children}</div>;
};
