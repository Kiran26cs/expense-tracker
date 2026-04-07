import { useState, useEffect } from 'react';
import { settingsApi } from '@/services/settings.api';
import type { Category, PaymentMethod } from '@/types';

export function useCategories() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [paymentMethods, setPaymentMethods] = useState<PaymentMethod[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      try {
        const [catRes, pmRes] = await Promise.all([
          settingsApi.getCategories(),
          settingsApi.getPaymentMethods(),
        ]);
        if (cancelled) return;
        if (catRes.success && catRes.data) setCategories(catRes.data);
        if (pmRes.success && pmRes.data) setPaymentMethods(pmRes.data);
      } catch { /* ignore */ }
      finally { if (!cancelled) setLoading(false); }
    };
    load();
    return () => { cancelled = true; };
  }, []);

  const categoryOptions = categories.map(c => ({ value: c.id, label: c.name }));
  const categoryFilterOptions = [{ value: 'all', label: 'All Categories' }, ...categoryOptions];
  const paymentMethodOptions = paymentMethods.map(p => ({ value: p.id, label: p.name }));

  const getCategoryById = (id: string) => categories.find(c => c.id === id);
  const getCategoryIcon = (idOrName: string) => {
    const cat = categories.find(c => c.id === idOrName || c.name === idOrName);
    return cat?.icon || 'fa-solid fa-receipt';
  };
  const getCategoryColor = (idOrName: string) => {
    const cat = categories.find(c => c.id === idOrName || c.name === idOrName);
    return cat?.color || '#6366f1';
  };
  const getCategoryName = (idOrName: string) => {
    const cat = categories.find(c => c.id === idOrName || c.name === idOrName);
    return cat?.name || idOrName;
  };

  return {
    categories, paymentMethods, loading,
    categoryOptions, categoryFilterOptions, paymentMethodOptions,
    getCategoryById, getCategoryIcon, getCategoryColor, getCategoryName,
  };
}
