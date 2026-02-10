import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/Card/Card';
import { Button } from '@/components/Button/Button';
import { Loading, EmptyState } from '@/components/Loading/Loading';
import { Modal } from '@/components/Modal/Modal';
import { Input } from '@/components/Input/Input';
import { Select } from '@/components/Select/Select';
import { useAuth } from '@/hooks/useAuth';
import { expenseBookService } from '@/services/expenseBookService';
import type { ExpenseBook, CreateExpenseBookRequest } from '@/types/expenseBook';
import styles from './ExpenseBookDashboard.module.css';

const ExpenseBookDashboard: React.FC = () => {
  const navigate = useNavigate();
  const { logout } = useAuth();
  const [expenseBooks, setExpenseBooks] = useState<ExpenseBook[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [formData, setFormData] = useState<CreateExpenseBookRequest>({
    name: '',
    description: '',
    category: 'Personal',
    isDefault: false,
  });
  const [isCustomCategory, setIsCustomCategory] = useState(false);
  const [customCategory, setCustomCategory] = useState('');

  useEffect(() => {
    loadExpenseBooks();
    loadCategories();
  }, []);

  const loadExpenseBooks = async () => {
    try {
      setLoading(true);
      const response = await expenseBookService.getExpenseBooks();
      if (response.success && response.data) {
        setExpenseBooks(response.data);
        setError(null);
      } else {
        setError(response.error || 'Failed to load expense books');
      }
    } catch (err) {
      setError('Failed to load expense books');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const loadCategories = async () => {
    try {
      const response = await expenseBookService.getCategories();
      if (response.success && response.data) {
        setCategories(response.data);
      }
    } catch (err) {
      console.error('Failed to load categories:', err);
    }
  };

  const handleCreateExpenseBook = async () => {
    try {
      const categoryToUse = isCustomCategory ? customCategory : formData.category;
      
      if (!formData.name.trim()) {
        alert('Please enter a name for the expense book');
        return;
      }

      if (isCustomCategory && !customCategory.trim()) {
        alert('Please enter a custom category');
        return;
      }

      const response = await expenseBookService.createExpenseBook({
        ...formData,
        category: categoryToUse,
      });

      if (response.success && response.data) {
        setExpenseBooks([response.data, ...expenseBooks]);
        setIsCreateDialogOpen(false);
        resetForm();

        // Navigate to the dashboard with the new expense book
        navigate(`/${response.data.id}/dashboard`);
      } else {
        alert(response.error || 'Failed to create expense book');
      }
    } catch (err) {
      console.error('Failed to create expense book:', err);
      alert('Failed to create expense book');
    }
  };

  const handleDeleteExpenseBook = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    
    if (!confirm('Are you sure you want to delete this expense book? This cannot be undone.')) {
      return;
    }

    try {
      const response = await expenseBookService.deleteExpenseBook(id);
      if (response.success) {
        setExpenseBooks(expenseBooks.filter(book => book.id !== id));
      } else {
        alert(response.error || 'Failed to delete expense book');
      }
    } catch (err: any) {
      const errorMessage = err.response?.data?.message || 'Failed to delete expense book';
      alert(errorMessage);
    }
  };

  const handleSelectExpenseBook = (bookId: string) => {
    navigate(`/${bookId}/dashboard`);
  };

  const resetForm = () => {
    setFormData({
      name: '',
      description: '',
      category: 'Personal',
      isDefault: false,
    });
    setIsCustomCategory(false);
    setCustomCategory('');
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  const getCategoryColor = (category: string) => {
    const colors: Record<string, string> = {
      Personal: '#4CAF50',
      Work: '#2196F3',
      Business: '#FF9800',
    };
    return colors[category] || '#9C27B0';
  };

  const getCreditCardGradient = (category: string) => {
    const gradients: Record<string, string> = {
      Personal: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      Work: 'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
      Business: 'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
    };
    return gradients[category] || 'linear-gradient(135deg, #fa709a 0%, #fee140 100%)';
  };

  if (loading) {
    return <Loading />;
  }

  return (
    <div className={styles.pageWrapper}>
      {/* Header */}
      <header className={styles.header}>
        <div className={styles.headerContent}>
          <div className={styles.logo}>
            <span className={styles.logoIcon}>💰</span>
            <span className={styles.logoText}>Expense Tracker</span>
          </div>
          <div className={styles.headerActions}>
            <Button variant="secondary" onClick={logout}>
              Logout
            </Button>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className={styles.mainContent}>
        <div className={styles.container}>
          <div className={styles.contentHeader}>
            <div>
              <h1 className={styles.title}>My Expense Books</h1>
              <p className={styles.subtitle}>Select an expense book to manage your finances</p>
            </div>
            <Button onClick={() => setIsCreateDialogOpen(true)}>
              + Book
            </Button>
          </div>

          {error && (
            <div className={styles.error}>{error}</div>
          )}

          {expenseBooks.length === 0 ? (
            <EmptyState
              icon="📚"
              title="No Expense Books Yet"
              description="Create your first expense book to start tracking your expenses"
              action={
                <Button onClick={() => setIsCreateDialogOpen(true)} variant="primary">
                  Create Expense Book
                </Button>
              }
            />
          ) : (
            <div className={styles.grid}>
              {expenseBooks.map((book) => (
                <div
                  key={book.id}
                  className={styles.creditCard}
                  onClick={() => handleSelectExpenseBook(book.id)}
                >
                  <div className={styles.cardGradient} style={{ background: getCreditCardGradient(book.category) }}>
                    {/* Card Header with Badge */}
                    <div className={styles.creditCardHeader}>
                      <div
                        className={styles.categoryBadge}
                        style={{ backgroundColor: getCategoryColor(book.category) }}
                      >
                        {book.category}
                      </div>
                      {book.isDefault && (
                        <span className={styles.defaultBadge}>Default</span>
                      )}
                    </div>

                    {/* Card Content */}
                    <div className={styles.creditCardContent}>
                      <div className={styles.bookNameWrapper}>
                        <h3 className={styles.bookName} title={book.name}>
                          {book.name}
                        </h3>
                      </div>

                      <div className={styles.cardStats}>
                        <div className={styles.statItem}>
                          <span className={styles.statValue}>
                            {formatCurrency(book.totalExpenses)}
                          </span>
                          <span className={styles.statLabel}>Total Spent</span>
                        </div>
                        <div className={styles.statDivider}></div>
                        <div className={styles.statItem}>
                          <span className={styles.statValue}>
                            {book.expenseCount}
                          </span>
                          <span className={styles.statLabel}>
                            {book.expenseCount === 1 ? 'Expense' : 'Expenses'}
                          </span>
                        </div>
                      </div>
                    </div>

                    {/* Hover Actions */}
                    <div className={styles.creditCardActions}>
                      <button
                        className={styles.actionBtn}
                        onClick={(e) => handleDeleteExpenseBook(book.id, e)}
                        disabled={book.isDefault && expenseBooks.length === 1}
                        title="Delete Book"
                      >
                        🗑️
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </main>

      {/* Footer */}
      <footer className={styles.footer}>
        <div className={styles.footerContent}>
          <p>© 2026 Expense Tracker. All rights reserved.</p>
        </div>
      </footer>

      <Modal
        isOpen={isCreateDialogOpen}
        onClose={() => {
          setIsCreateDialogOpen(false);
          resetForm();
        }}
        title="Create New Expense Book"
      >
        <div className={styles.form}>
          <Input
            label="Name"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            placeholder="e.g., Personal Expenses, Work Expenses"
            required
          />

          <div className={styles.formGroup}>
            <label htmlFor="description">Description</label>
            <textarea
              id="description"
              className={styles.textarea}
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              placeholder="Optional description"
              rows={3}
            />
          </div>

          {!isCustomCategory ? (
            <div className={styles.formGroup}>
              <Select
                label="Category"
                value={formData.category}
                onChange={(e) => setFormData({ ...formData, category: e.target.value })}
                options={categories.map((category) => ({ value: category, label: category }))}
              />
              <button
                type="button"
                className={styles.linkBtn}
                onClick={() => setIsCustomCategory(true)}
              >
                + Add Custom Category
              </button>
            </div>
          ) : (
            <div className={styles.formGroup}>
              <Input
                label="Custom Category"
                value={customCategory}
                onChange={(e) => setCustomCategory(e.target.value)}
                placeholder="Enter custom category name"
              />
              <button
                type="button"
                className={styles.linkBtn}
                onClick={() => {
                  setIsCustomCategory(false);
                  setCustomCategory('');
                }}
              >
                Use Existing Category
              </button>
            </div>
          )}

          <div className={styles.modalActions}>
            <Button
              variant="secondary"
              onClick={() => {
                setIsCreateDialogOpen(false);
                resetForm();
              }}
            >
              Cancel
            </Button>
            <Button
              variant="primary"
              onClick={handleCreateExpenseBook}
              disabled={!formData.name.trim() || (isCustomCategory && !customCategory.trim())}
            >
              Create
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default ExpenseBookDashboard;
