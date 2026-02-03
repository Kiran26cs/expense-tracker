import { useState, ReactNode } from 'react';
import { Sidebar } from '@/components/Sidebar/Sidebar';
import { TopBar } from '@/components/TopBar/TopBar';
import { useBreakpoint } from '@/hooks/useMediaQuery';
import styles from './AppLayout.module.css';

interface AppLayoutProps {
  children: ReactNode;
}

export const AppLayout = ({ children }: AppLayoutProps) => {
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
  const { isMobile } = useBreakpoint();

  const handleSearch = (query: string) => {
    console.log('Search query:', query);
    // Implement global search logic here
  };

  return (
    <div className={styles['app-layout']}>
      <Sidebar isCollapsed={isSidebarCollapsed} onCollapsedChange={setIsSidebarCollapsed} />
      <TopBar isSidebarCollapsed={isSidebarCollapsed} onSearch={handleSearch} />
      <main
        className={`${styles['main-content']} ${
          isSidebarCollapsed && !isMobile ? styles['sidebar-collapsed'] : ''
        }`}
      >
        {children}
      </main>
    </div>
  );
};
