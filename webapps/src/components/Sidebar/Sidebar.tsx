import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
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
  { id: 'dashboard', label: 'Dashboard', icon: '📊', path: '/' },
  { id: 'expenses', label: 'Expenses', icon: '💰', path: '/expenses' },
  { id: 'budget', label: 'Budget', icon: '🎯', path: '/budget' },
  { id: 'insights', label: 'Insights', icon: '📈', path: '/insights' },
  { id: 'settings', label: 'Settings', icon: '⚙️', path: '/settings' },
];

export const Sidebar = ({ isCollapsed = false, onCollapsedChange }: SidebarProps) => {
  const location = useLocation();
  const { isMobile } = useBreakpoint();

  const toggleCollapse = () => {
    onCollapsedChange?.(!isCollapsed);
  };

  const isActive = (path: string) => {
    if (path === '/') return location.pathname === '/';
    return location.pathname.startsWith(path);
  };

  return (
    <aside className={`${styles.sidebar} ${isCollapsed && !isMobile ? styles.collapsed : ''}`}>
      {!isMobile && (
        <div className={styles['sidebar-header']}>
          <div className={styles['sidebar-logo']}>E</div>
          <h1 className={styles['sidebar-title']}>ExpenseTracker</h1>
        </div>
      )}

      <nav className={styles['sidebar-nav']}>
        {navItems.map((item) => (
          <Link
            key={item.id}
            to={item.path}
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
