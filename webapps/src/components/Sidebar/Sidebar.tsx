import { useState, useEffect } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { useBreakpoint } from '@/hooks/useMediaQuery';
import styles from './Sidebar.module.css';

interface NavItem {
  id: string;
  label: string;
  icon: string;
  path: string;
}

interface SidebarProps {
  isCollapsed?: boolean;
  onCollapsedChange?: (collapsed: boolean) => void;
}

const navItems: NavItem[] = [
  { id: 'dashboard', label: 'Dashboard', icon: '📊', path: 'dashboard' },
  { id: 'expenses', label: 'Expenses', icon: '💰', path: 'expenses' },
  { id: 'budget', label: 'Budget', icon: '🎯', path: 'budget' },
  { id: 'insights', label: 'Insights', icon: '📈', path: 'insights' },
  { id: 'settings', label: 'Settings', icon: '⚙️', path: 'settings' },
];

export const Sidebar = ({ isCollapsed = false, onCollapsedChange }: SidebarProps) => {
  const location = useLocation();
  const { bookId } = useParams<{ bookId: string }>();
  const { isMobile } = useBreakpoint();

  const toggleCollapse = () => {
    onCollapsedChange?.(!isCollapsed);
  };

  const isActive = (path: string) => {
    return location.pathname.includes(`/${path}`);
  };

  const getNavPath = (path: string) => {
    return `/${bookId}/${path}`;
  };

  return (
    <aside className={`${styles.sidebar} ${isCollapsed && !isMobile ? styles.collapsed : ''}`}>
      {!isMobile && (
        <Link to="/" className={styles['sidebar-header']} style={{ textDecoration: 'none', color: 'inherit' }}>
          <div className={styles['sidebar-logo']}>E</div>
          <h1 className={styles['sidebar-title']}>ExpenseTracker</h1>
        </Link>
      )}

      <nav className={styles['sidebar-nav']}>
        {navItems.map((item) => (
          <Link
            key={item.id}
            to={getNavPath(item.path)}
            className={`${styles['nav-item']} ${isActive(item.path) ? styles.active : ''}`}
          >
            <span className={styles['nav-item-icon']} role="img" aria-label={item.label}>
              {item.icon}
            </span>
            <span className={styles['nav-item-text']}>{item.label}</span>
          </Link>
        ))}
      </nav>

      {!isMobile && (
        <div className={styles['sidebar-footer']}>
          <button
            className={styles['collapse-button']}
            onClick={toggleCollapse}
            aria-label={isCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          >
            {isCollapsed ? '→' : '←'}
          </button>
        </div>
      )}
    </aside>
  );
};
