import { useState, useEffect, useRef } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { useTheme } from '@/hooks/useTheme';
import { useBreakpoint } from '@/hooks/useMediaQuery';
import styles from './TopBar.module.css';

interface TopBarProps {
  isSidebarCollapsed?: boolean;
  onSearch?: (query: string) => void;
  bookName?: string;
}

export const TopBar = ({ isSidebarCollapsed = false, onSearch, bookName }: TopBarProps) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const { user, logout } = useAuth();
  const { theme, toggleTheme } = useTheme();
  const { isMobile } = useBreakpoint();

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsUserMenuOpen(false);
      }
    };

    if (isUserMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isUserMenuOpen]);

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const query = e.target.value;
    setSearchQuery(query);
    onSearch?.(query);
  };

  const handleLogout = async () => {
    await logout();
  };

  const getUserInitials = () => {
    if (!user?.name) return 'U';
    return user.name
      .split(' ')
      .map((n) => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  };

  return (
    <header
      className={`${styles.topbar} ${
        isSidebarCollapsed && !isMobile ? styles['sidebar-collapsed'] : ''
      }`}
    >
      <div className={styles['topbar-left']}>
        {isMobile && (
          <button className={styles['mobile-menu-button']} aria-label="Menu">
            ☰
          </button>
        )}
        <Link to="/" style={{ textDecoration: 'none', color: 'inherit' }}>
          <h2 className={styles['app-name']}>{bookName || 'ExpenseTracker'}</h2>
        </Link>
      </div>

      {!isMobile && (
        <div className={styles['topbar-center']}>
          <div className={styles['search-bar']}>
            <span className={styles['search-icon']}>🔍</span>
            <input
              type="text"
              className={styles['search-input']}
              placeholder="Search transactions, categories..."
              value={searchQuery}
              onChange={handleSearchChange}
            />
          </div>
        </div>
      )}

      <div className={styles['topbar-right']}>
        <button
          className={styles['icon-button']}
          onClick={toggleTheme}
          aria-label="Toggle theme"
        >
          {theme === 'light' ? '🌙' : '☀️'}
        </button>

        <button className={styles['icon-button']} aria-label="Notifications">
          🔔
          <span className={styles['notification-badge']}></span>
        </button>

        <div className={styles.dropdown} ref={dropdownRef}>
          <div
            className={styles['user-avatar']}
            onClick={() => setIsUserMenuOpen(!isUserMenuOpen)}
          >
            {user?.avatar ? (
              <img src={user.avatar} alt={user.name} />
            ) : (
              getUserInitials()
            )}
          </div>

          {isUserMenuOpen && (
            <div className={styles['dropdown-menu']}>
              <div className={styles['dropdown-header']}>
                <div className={styles['dropdown-user-info']}>
                  <div className={styles['dropdown-avatar']}>{getUserInitials()}</div>
                  <div>
                    <div className={styles['dropdown-user-name']}>{user?.name || 'User'}</div>
                    <div className={styles['dropdown-user-email']}>{user?.email || ''}</div>
                  </div>
                </div>
              </div>
              <div className={styles['dropdown-divider']}></div>
              <a href="/settings" className={styles['dropdown-item']}>
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <circle cx="12" cy="12" r="3"/>
                  <path d="M12 1v6m0 6v6M5.64 5.64l4.24 4.24m4.24 4.24l4.24 4.24M1 12h6m6 0h6M5.64 18.36l4.24-4.24m4.24-4.24l4.24-4.24"/>
                </svg>
                <span>Settings</span>
              </a>
              <a href="/help" className={styles['dropdown-item']}>
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <circle cx="12" cy="12" r="10"/>
                  <path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"/>
                  <line x1="12" y1="17" x2="12.01" y2="17"/>
                </svg>
                <span>Help</span>
              </a>
              <div className={styles['dropdown-divider']}></div>
              <button className={`${styles['dropdown-item']} ${styles['dropdown-item-danger']}`} onClick={handleLogout}>
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/>
                  <polyline points="16 17 21 12 16 7"/>
                  <line x1="21" y1="12" x2="9" y2="12"/>
                </svg>
                <span>Logout</span>
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};
