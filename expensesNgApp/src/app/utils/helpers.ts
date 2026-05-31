export const CURRENCY_OPTIONS = [
  { value: 'USD', label: 'USD – US Dollar' },
  { value: 'EUR', label: 'EUR – Euro' },
  { value: 'GBP', label: 'GBP – British Pound' },
  { value: 'INR', label: 'INR – Indian Rupee' },
  { value: 'JPY', label: 'JPY – Japanese Yen' },
  { value: 'AUD', label: 'AUD – Australian Dollar' },
  { value: 'CAD', label: 'CAD – Canadian Dollar' },
  { value: 'CHF', label: 'CHF – Swiss Franc' },
  { value: 'CNY', label: 'CNY – Chinese Yuan' },
  { value: 'SGD', label: 'SGD – Singapore Dollar' },
  { value: 'AED', label: 'AED – UAE Dirham' },
  { value: 'MYR', label: 'MYR – Malaysian Ringgit' },
];

export const BOOK_ICON_OPTIONS = [
  { value: 'fa fa-book',           label: 'Book' },
  { value: 'fa fa-wallet',         label: 'Wallet' },
  { value: 'fa fa-home',           label: 'Home' },
  { value: 'fa fa-briefcase',      label: 'Business' },
  { value: 'fa fa-shopping-cart',  label: 'Shopping' },
  { value: 'fa fa-car',            label: 'Car' },
  { value: 'fa fa-utensils',       label: 'Food & Dining' },
  { value: 'fa fa-heart',          label: 'Personal' },
  { value: 'fa fa-plane',          label: 'Travel' },
  { value: 'fa fa-graduation-cap', label: 'Education' },
  { value: 'fa fa-medkit',         label: 'Health' },
  { value: 'fa fa-bolt',           label: 'Utilities' },
  { value: '__other__',            label: 'Other (custom)' },
];

export const CATEGORY_FA_ICONS = [
  { icon: 'fa-solid fa-utensils',      label: 'Food' },
  { icon: 'fa-solid fa-car',           label: 'Transport' },
  { icon: 'fa-solid fa-bag-shopping',  label: 'Shopping' },
  { icon: 'fa-solid fa-bolt',          label: 'Utilities' },
  { icon: 'fa-solid fa-film',          label: 'Entertainment' },
  { icon: 'fa-solid fa-heart-pulse',   label: 'Health' },
  { icon: 'fa-solid fa-graduation-cap', label: 'Education' },
  { icon: 'fa-solid fa-house',         label: 'Rent' },
  { icon: 'fa-solid fa-cart-shopping', label: 'Groceries' },
  { icon: 'fa-solid fa-plane',         label: 'Travel' },
  { icon: 'fa-solid fa-gamepad',       label: 'Gaming' },
  { icon: 'fa-solid fa-shirt',         label: 'Clothing' },
  { icon: 'fa-solid fa-mobile-screen', label: 'Phone' },
  { icon: 'fa-solid fa-dumbbell',      label: 'Fitness' },
  { icon: 'fa-solid fa-gift',          label: 'Gifts' },
  { icon: 'fa-solid fa-baby',          label: 'Kids' },
  { icon: 'fa-solid fa-paw',           label: 'Pets' },
  { icon: 'fa-solid fa-briefcase',     label: 'Work' },
  { icon: 'fa-solid fa-piggy-bank',    label: 'Savings' },
  { icon: 'fa-solid fa-ellipsis',      label: 'Other' },
];

export const CATEGORY_COLOR_OPTIONS = [
  '#ef4444', '#f97316', '#f59e0b', '#22c55e',
  '#10b981', '#14b8a6', '#3b82f6', '#6366f1',
  '#8b5cf6', '#ec4899', '#64748b', '#0ea5e9',
];

export const formatCurrency = (amount: number, currency = 'USD'): string => {
  const locale = currency === 'INR' ? 'en-IN' : 'en-US';
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(amount);
};

const userLocale = (): string => navigator.language || 'en';

/**
 * Returns today's date (or the given date) as a YYYY-MM-DD string in the
 * user's LOCAL timezone — safe replacement for new Date().toISOString().split('T')[0]
 * which returns the UTC date and can be off by a day for UTC± users.
 */
export const localDateString = (d: Date = new Date()): string => {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
};

/**
 * Formats a calendar-date field (expense date, due date, budget period, etc.).
 * These are stored as UTC-midnight of the user's chosen local date, so we must
 * format them in the UTC timezone to prevent date shifting for UTC± users.
 */
export const formatCalendarDate = (date: string | Date, format: 'short' | 'long' | 'relative' = 'short'): string => {
  const d = typeof date === 'string' ? new Date(date) : date;
  if (format === 'relative') return getRelativeTime(d);
  const opts: Intl.DateTimeFormatOptions = format === 'long'
    ? { year: 'numeric', month: 'long', day: 'numeric', timeZone: 'UTC' }
    : { year: 'numeric', month: 'short', day: 'numeric', timeZone: 'UTC' };
  return new Intl.DateTimeFormat(userLocale(), opts).format(d);
};

/**
 * Formats a precise timestamp (createdAt, updatedAt, recordedAt).
 * Converts from UTC to the user's local timezone so the local time is shown.
 */
export const formatTimestamp = (date: string | Date, format: 'short' | 'long' | 'relative' | 'datetime' = 'short'): string => {
  const d = typeof date === 'string' ? new Date(date) : date;
  if (format === 'relative') return getRelativeTime(d);
  const tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
  if (format === 'datetime') {
    return new Intl.DateTimeFormat(userLocale(), { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit', timeZone: tz }).format(d);
  }
  const opts: Intl.DateTimeFormatOptions = format === 'long'
    ? { year: 'numeric', month: 'long', day: 'numeric', timeZone: tz }
    : { year: 'numeric', month: 'short', day: 'numeric', timeZone: tz };
  return new Intl.DateTimeFormat(userLocale(), opts).format(d);
};

/** @deprecated Use formatCalendarDate for expense/due dates, formatTimestamp for createdAt/updatedAt */
export const formatDate = formatCalendarDate;

export const getRelativeTime = (date: Date): string => {
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);
  if (diffInSeconds < 60) return 'Just now';
  if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
  if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
  if (diffInSeconds < 604800) return `${Math.floor(diffInSeconds / 86400)}d ago`;
  if (diffInSeconds < 2592000) return `${Math.floor(diffInSeconds / 604800)}w ago`;
  if (diffInSeconds < 31536000) return `${Math.floor(diffInSeconds / 2592000)}mo ago`;
  return `${Math.floor(diffInSeconds / 31536000)}y ago`;
};

export const calculatePercentage = (value: number, total: number): number => {
  if (total === 0) return 0;
  return Math.round((value / total) * 100);
};

export const isValidEmail = (email: string): boolean => {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
};

export const isValidPhone = (phone: string): boolean => {
  return /^\+?[\d\s\-()]{8,15}$/.test(phone);
};

export const truncateText = (text: string, maxLength: number): string => {
  if (text.length <= maxLength) return text;
  return text.slice(0, maxLength) + '...';
};

export const debounce = <T extends (...args: any[]) => any>(func: T, wait: number) => {
  let timeout: ReturnType<typeof setTimeout> | null = null;
  return (...args: Parameters<T>) => {
    if (timeout) clearTimeout(timeout);
    timeout = setTimeout(() => func(...args), wait);
  };
};

export const getCategoryIcon = (category: string): string => {
  const icons: Record<string, string> = {
    food: 'fa-solid fa-utensils', transport: 'fa-solid fa-car', shopping: 'fa-solid fa-bag-shopping',
    bills: 'fa-solid fa-file-invoice-dollar', entertainment: 'fa-solid fa-film',
  };
  return icons[category?.toLowerCase()] || 'fa-solid fa-tag';
};

export const getCategoryColor = (category: string): string => {
  const colors: Record<string, string> = {
    food: '#6366f1', transport: '#ef4444', shopping: '#f97316',
    bills: '#8b5cf6', entertainment: '#ec4899',
  };
  return colors[category?.toLowerCase()] || '#6366f1';
};
