import { useState } from 'react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/Card/Card';
import { Button } from '@/components/Button/Button';
import { Input } from '@/components/Input/Input';
import { Select } from '@/components/Select/Select';
import { useTheme } from '@/hooks/useTheme';

export const SettingsPage = () => {
  const { theme, toggleTheme } = useTheme();
  const [currency, setCurrency] = useState('INR');
  const [minimumSavings, setMinimumSavings] = useState('5000');

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
            <p style={{ color: 'var(--color-text-secondary)', marginBottom: '1rem' }}>
              Manage your expense categories
            </p>
            <Button>Manage Categories</Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Import Data</CardTitle>
          </CardHeader>
          <CardContent>
            <p style={{ color: 'var(--color-text-secondary)', marginBottom: '1rem' }}>
              Import expenses from CSV file
            </p>
            <Button>Import CSV</Button>
          </CardContent>
        </Card>

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
