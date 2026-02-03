import { useState } from 'react';
import { useAuth } from '@/hooks/useAuth';
import { useTheme } from '@/hooks/useTheme';
import { useBreakpoint } from '@/hooks/useMediaQuery';
import styles from './TopBar.module.css';

interface TopBarProps {
  isSidebarCollapsed?: boolean;
  onSearch?: (query: string) => void;
}

export const TopBar = ({ isSidebarCollapsed = false, onSearch }: TopBarProps) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
  const { user, logout } = useAuth();
  const { theme, toggleTheme } = useTheme();
  const { isMobile } = useBreakpoint();

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
        <h2 className={styles['app-name']}>ExpenseTracker</h2>
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

        <div className={styles.dropdown}>
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
              <div className={styles['dropdown-item']}>
                <span>👤</span>
                <span>{user?.name || 'User'}</span>
              </div>
              <div className={styles['dropdown-divider']}></div>
              <a href="/settings" className={styles['dropdown-item']}>
                <span>⚙️</span>
                <span>Settings</span>
              </a>
              <a href="/help" className={styles['dropdown-item']}>
                <span>❓</span>
                <span>Help</span>
              </a>
              <div className={styles['dropdown-divider']}></div>
              <button className={styles['dropdown-item']} onClick={handleLogout}>
                <span>🚪</span>
                <span>Logout</span>
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};
