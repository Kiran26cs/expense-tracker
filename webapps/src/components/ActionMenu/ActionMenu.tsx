import { useState, useRef, useEffect } from 'react';
import styles from './ActionMenu.module.css';

interface ActionMenuProps {
  onAddExpense: () => void;
  onImportCSV: () => void;
}

export const ActionMenu = ({ onAddExpense, onImportCSV }: ActionMenuProps) => {
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const [dropdownPosition, setDropdownPosition] = useState({ top: 0, right: 0 });

  useEffect(() => {
    if (isOpen && buttonRef.current && dropdownRef.current) {
      const buttonRect = buttonRef.current.getBoundingClientRect();
      
      // Position dropdown below button
      setDropdownPosition({
        top: buttonRect.bottom + 8,
        right: window.innerWidth - buttonRect.right,
      });
    }
  }, [isOpen]);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  return (
    <div ref={menuRef} className={styles['action-menu']}>
      <button
        ref={buttonRef}
        className={styles['menu-button']}
        onClick={() => setIsOpen(!isOpen)}
        title="Action menu"
      >
        + Add Expense
        <span className={styles['dropdown-icon']}>▼</span>
      </button>

      {isOpen && (
        <div
          ref={dropdownRef}
          className={styles['menu-dropdown']}
          style={{
            top: `${dropdownPosition.top}px`,
            right: `${dropdownPosition.right}px`,
          }}
        >
          <button
            className={styles['menu-item']}
            onClick={() => {
              onAddExpense();
              setIsOpen(false);
            }}
          >
            <span className={styles.icon}><i className="fa-solid fa-plus" /></span>
            <div className={styles['item-content']}>
              <div className={styles['item-title']}>Add Expense</div>
              <div className={styles['item-description']}>Add a single expense</div>
            </div>
          </button>

          <button
            className={styles['menu-item']}
            onClick={() => {
              onImportCSV();
              setIsOpen(false);
            }}
          >
            <span className={styles.icon}><i className="fa-solid fa-file-import" /></span>
            <div className={styles['item-content']}>
              <div className={styles['item-title']}>Import from CSV</div>
              <div className={styles['item-description']}>Bulk import multiple expenses</div>
            </div>
          </button>
        </div>
      )}
    </div>
  );
};
