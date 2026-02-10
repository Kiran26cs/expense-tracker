import { useState, useEffect, ReactNode } from 'react';
import { useParams } from 'react-router-dom';
import { Sidebar } from '@/components/Sidebar/Sidebar';
import { TopBar } from '@/components/TopBar/TopBar';
import { useBreakpoint } from '@/hooks/useMediaQuery';
import { expenseBookService } from '@/services/expenseBookService';
import styles from './AppLayout.module.css';

interface AppLayoutProps {
  children: ReactNode;
}

export const AppLayout = ({ children }: AppLayoutProps) => {
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
  const [bookName, setBookName] = useState<string | undefined>();
  const { isMobile } = useBreakpoint();
  const { bookId } = useParams<{ bookId: string }>();

  useEffect(() => {
    const fetchBookName = async () => {
      if (bookId) {
        try {
          const response = await expenseBookService.getExpenseBookById(bookId);
          if (response.success && response.data) {
            setBookName(response.data.name);
          }
        } catch (error) {
          console.error('Failed to fetch expense book:', error);
        }
      } else {
        setBookName(undefined);
      }
    };

    fetchBookName();
  }, [bookId]);

  const handleSearch = (query: string) => {
    console.log('Search query:', query);
    // Implement global search logic here
  };

  return (
    <div className={styles['app-layout']}>
      <Sidebar isCollapsed={isSidebarCollapsed} onCollapsedChange={setIsSidebarCollapsed} />
      <TopBar isSidebarCollapsed={isSidebarCollapsed} onSearch={handleSearch} bookName={bookName} />
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
