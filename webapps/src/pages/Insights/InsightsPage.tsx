import { Card, CardHeader, CardTitle, CardContent } from '@/components/Card/Card';
import { Button } from '@/components/Button/Button';
import { EmptyState } from '@/components/Loading/Loading';

export const InsightsPage = () => {
  return (
    <div>
      <h1 style={{ fontSize: '2rem', fontWeight: 'bold', marginBottom: '2rem' }}>Insights & Forecast</h1>

      <div style={{ display: 'grid', gap: '1.5rem' }}>
        <Card>
          <CardHeader>
            <CardTitle>Cash Runway</CardTitle>
          </CardHeader>
          <CardContent>
            <div style={{ textAlign: 'center', padding: '2rem' }}>
              <div style={{ fontSize: '3rem', fontWeight: 'bold', color: 'var(--color-success)' }}>45 days</div>
              <p style={{ color: 'var(--color-text-secondary)', marginTop: '0.5rem' }}>
                Based on your current spending patterns
              </p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Cash Flow Forecast</CardTitle>
          </CardHeader>
          <CardContent>
            <EmptyState
              icon="📈"
              title="Forecast unavailable"
              description="Add more expenses to generate accurate forecasts"
            />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>"Can I Buy This?" Simulator</CardTitle>
          </CardHeader>
          <CardContent>
            <p style={{ color: 'var(--color-text-secondary)', marginBottom: '1rem' }}>
              Check if a purchase will impact your budget and savings goals
            </p>
            <Button>Open Simulator</Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Recurring Expenses</CardTitle>
          </CardHeader>
          <CardContent>
            <EmptyState
              icon="🔄"
              title="No recurring expenses"
              description="Set up recurring expenses for subscriptions and regular bills"
            />
          </CardContent>
        </Card>
      </div>
    </div>
  );
};
