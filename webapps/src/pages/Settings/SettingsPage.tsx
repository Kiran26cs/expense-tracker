import { useState, useEffect } from 'react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/Card/Card';
import { Button } from '@/components/Button/Button';
import { Input } from '@/components/Input/Input';
import { Select } from '@/components/Select/Select';
import { useTheme } from '@/hooks/useTheme';
import { CategoryManagementModal } from '@/components/CategoryManagementModal';
import { ImportCSVModal } from '@/components/ImportCSV/ImportCSVModal';
import { settingsApi } from '@/services/settings.api';

export const SettingsPage = () => {
  const { theme, toggleTheme } = useTheme();
  const [currency, setCurrency] = useState('INR');
  const [minimumSavings, setMinimumSavings] = useState('5000');
  const [isCategoryModalOpen, setIsCategoryModalOpen] = useState(false);
  const [isImportModalOpen, setIsImportModalOpen] = useState(false);
  const [categoryCount, setCategoryCount] = useState(0);

  useEffect(() => {
    fetchCategoryCount();
  }, []);

  const fetchCategoryCount = async () => {
    try {
      const response = await settingsApi.getCategories();
      if (response.success && response.data) {
        setCategoryCount(response.data.length);
      }
    } catch (error) {
      console.error('Failed to fetch categories:', error);
    }
  };

  return (
    <div style={{ maxWidth: '800px' }}>
      <h1 style={{ fontSize: '2rem', fontWeight: 'bold', marginBottom: '2rem' }}>Settings</h1>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
        <Card>
          <CardHeader>
            <CardTitle>Appearance</CardTitle>
          </CardHeader>
          <CardContent>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <div style={{ fontWeight: 500 }}>Theme</div>
                <div style={{ fontSize: '0.875rem', color: 'var(--color-text-secondary)', marginTop: '0.25rem' }}>
                  Current: {theme === 'light' ? 'Light' : 'Dark'}
                </div>
              </div>
              <Button onClick={toggleTheme}>
                {theme === 'light' ? '🌙 Dark Mode' : '☀️ Light Mode'}
              </Button>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Financial Settings</CardTitle>
          </CardHeader>
          <CardContent>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <Select
                label="Currency"
                options={[
                  { value: 'INR', label: '₹ Indian Rupee (INR)' },
                  { value: 'USD', label: '$ US Dollar (USD)' },
                  { value: 'EUR', label: '€ Euro (EUR)' },
                  { value: 'GBP', label: '£ British Pound (GBP)' },
                ]}
                value={currency}
                onChange={(e) => setCurrency(e.target.value)}
              />

              <Input
                label="Minimum Monthly Savings"
                type="number"
                value={minimumSavings}
                onChange={(e) => setMinimumSavings(e.target.value)}
                placeholder="Enter amount"
              />

              <Button style={{ alignSelf: 'flex-start' }}>Save Changes</Button>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Categories</CardTitle>
          </CardHeader>
          <CardContent>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '1rem' }}>
              <p style={{ color: 'var(--color-text-secondary)' }}>
                Manage your expense categories
              </p>
              <div style={{ 
                padding: '0.25rem 0.75rem', 
                backgroundColor: 'var(--color-primary-light, rgba(139, 92, 246, 0.1))',
                color: 'var(--color-primary)',
                borderRadius: '1rem',
                fontSize: '0.875rem',
                fontWeight: '600'
              }}>
                {categoryCount} {categoryCount === 1 ? 'Category' : 'Categories'}
              </div>
            </div>
            <Button onClick={() => setIsCategoryModalOpen(true)}>Manage Categories</Button>
          </CardContent>
        </Card>

        <CategoryManagementModal 
          isOpen={isCategoryModalOpen} 
          onClose={() => setIsCategoryModalOpen(false)}
          onSuccess={fetchCategoryCount}
        />

        <Card>
          <CardHeader>
            <CardTitle>Import Data</CardTitle>
          </CardHeader>
          <CardContent>
            <p style={{ color: 'var(--color-text-secondary)', marginBottom: '1rem' }}>
              Add expenses manually or import from CSV file
            </p>
            <Button onClick={() => setIsImportModalOpen(true)}>
              <i className="fa-solid fa-file-import" style={{ marginRight: '0.5rem' }} />
              Add / Import Expenses
            </Button>
          </CardContent>
        </Card>

        <ImportCSVModal
          isOpen={isImportModalOpen}
          onClose={() => setIsImportModalOpen(false)}
          onSuccess={() => setIsImportModalOpen(false)}
        />

        <Card>
          <CardHeader>
            <CardTitle>Account</CardTitle>
          </CardHeader>
          <CardContent>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
              <Button variant="ghost" style={{ justifyContent: 'flex-start' }}>
                Change Email/Phone
              </Button>
              <Button variant="ghost" style={{ justifyContent: 'flex-start' }}>
                Export Data
              </Button>
              <Button variant="danger" style={{ justifyContent: 'flex-start' }}>
                Delete Account
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};
