import { useState, useRef } from 'react';
import { Button } from '@/components/Button/Button';
import { expenseApi } from '@/services/expense.api';
import styles from './ImportCSVModal.module.css';

interface ImportCSVModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (count: number) => void;
}

export const ImportCSVModal = ({ isOpen, onClose, onSuccess }: ImportCSVModalProps) => {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [preview, setPreview] = useState<any[]>([]);
  const [importStats, setImportStats] = useState<{ success: number; failed: number } | null>(null);

  if (!isOpen) return null;

  const downloadTemplate = () => {
    const csvContent = `description,amount,date,category,paymentMethod,notes
Grocery Shopping,500,2026-02-03,food,cash,Weekly shopping at supermarket
Gas Refill,1000,2026-02-02,transport,card,Monthly fuel
Movie Tickets,400,2026-02-01,entertainment,upi,Movie night with friends
Electric Bill,2500,2026-01-31,bills,bank,Monthly electricity bill
Coffee,150,2026-01-30,food,cash,Coffee at cafe`;

    const element = document.createElement('a');
    element.setAttribute('href', 'data:text/csv;charset=utf-8,' + encodeURIComponent(csvContent));
    element.setAttribute('download', 'expense_template.csv');
    element.style.display = 'none';
    document.body.appendChild(element);
    element.click();
    document.body.removeChild(element);
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (!selectedFile) return;

    if (!selectedFile.name.endsWith('.csv')) {
      setError('Please select a CSV file');
      return;
    }

    setFile(selectedFile);
    setError('');
    setPreview([]);

    // Parse and preview the file
    try {
      const text = await selectedFile.text();
      const lines = text.trim().split('\n');
      const headers = lines[0].split(',').map(h => h.trim());
      
      // Validate headers
      const requiredHeaders = ['description', 'amount', 'date', 'category', 'paymentMethod'];
      const missingHeaders = requiredHeaders.filter(h => !headers.includes(h));
      
      if (missingHeaders.length > 0) {
        setError(`Missing required columns: ${missingHeaders.join(', ')}`);
        setFile(null);
        return;
      }

      // Parse data rows (max 10 for preview)
      const previewData = lines.slice(1, 11).map((line, idx) => {
        const values = line.split(',').map(v => v.trim());
        const row: any = {};
        headers.forEach((header, i) => {
          row[header] = values[i] || '';
        });
        row._rowNumber = idx + 2;
        return row;
      });

      setPreview(previewData);
    } catch (err) {
      setError('Failed to parse CSV file');
      setFile(null);
    }
  };

  const handleImport = async () => {
    if (!file) {
      setError('Please select a file');
      return;
    }

    setLoading(true);
    setError('');

    try {
      const text = await file.text();
      const lines = text.trim().split('\n');
      const headers = lines[0].split(',').map(h => h.trim());

      const expenses: any[] = [];
      let failedCount = 0;

      // Parse all data rows
      for (let i = 1; i < lines.length; i++) {
        try {
          const values = lines[i].split(',').map(v => v.trim());
          const row: any = {};
          headers.forEach((header, idx) => {
            row[header] = values[idx] || '';
          });

          // Validate required fields
          if (!row.description || !row.amount || !row.date || !row.category || !row.paymentMethod) {
            failedCount++;
            continue;
          }

          // Parse and validate amount
          const amount = parseFloat(row.amount);
          if (isNaN(amount) || amount <= 0) {
            failedCount++;
            continue;
          }

          // Parse and validate date
          const dateObj = new Date(row.date);
          if (isNaN(dateObj.getTime())) {
            failedCount++;
            continue;
          }

          expenses.push({
            description: row.description,
            amount,
            date: dateObj.toISOString(),
            category: row.category.toLowerCase(),
            paymentMethod: row.paymentMethod.toLowerCase(),
            notes: row.notes || undefined,
            isRecurring: false,
          });
        } catch (err) {
          failedCount++;
        }
      }

      if (expenses.length === 0) {
        setError('No valid expenses found in the file');
        setLoading(false);
        return;
      }

      // Import all expenses (send one by one or batch if backend supports)
      let successCount = 0;
      for (const expense of expenses) {
        try {
          const response = await expenseApi.createExpense(expense);
          if (response.success) {
            successCount++;
          } else {
            failedCount++;
          }
        } catch (err) {
          failedCount++;
        }
      }

      setImportStats({ success: successCount, failed: failedCount });
      
      if (successCount > 0) {
        setTimeout(() => {
          onSuccess(successCount);
        }, 1500);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Import failed');
    } finally {
      setLoading(false);
    }
  };

  const handleClose = () => {
    setFile(null);
    setError('');
    setPreview([]);
    setImportStats(null);
    onClose();
  };

  return (
    <div className={styles.overlay}>
      <div className={styles.modal}>
        <div className={styles.header}>
          <h2>Import Expenses from CSV</h2>
          <button
            className={styles['close-btn']}
            onClick={handleClose}
            type="button"
          >
            ✕
          </button>
        </div>

        <div className={styles.content}>
          {importStats ? (
            <div className={styles['import-result']}>
              <div className={styles['success-icon']}>✅</div>
              <h3>Import Complete!</h3>
              <p className={styles['stat-row']}>
                <span className={styles.label}>Successfully imported:</span>
                <span className={styles['success-count']}>{importStats.success} expenses</span>
              </p>
              {importStats.failed > 0 && (
                <p className={styles['stat-row']}>
                  <span className={styles.label}>Failed:</span>
                  <span className={styles['failed-count']}>{importStats.failed} rows</span>
                </p>
              )}
              <p className={styles.info}>Your expenses will appear in the list shortly.</p>
            </div>
          ) : (
            <>
              <div className={styles.section}>
                <h3>1. Download Template</h3>
                <p>Start with the CSV template to understand the required format.</p>
                <Button onClick={downloadTemplate} variant="ghost" fullWidth>
                  📥 Download CSV Template
                </Button>
              </div>

              <div className={styles.section}>
                <h3>2. Select File</h3>
                <p>Click to select your CSV file or drag and drop below.</p>
                <div className={styles['file-input-wrapper']}>
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept=".csv"
                    onChange={handleFileChange}
                    className={styles['file-input']}
                  />
                  <div
                    className={styles['file-drop']}
                    onClick={() => fileInputRef.current?.click()}
                  >
                    <div>📄 {file ? file.name : 'Click to select CSV or drag and drop'}</div>
                  </div>
                </div>
              </div>

              {preview.length > 0 && (
                <div className={styles.section}>
                  <h3>3. Preview</h3>
                  <p>First few rows of your file:</p>
                  <div className={styles['preview-table']}>
                    <table>
                      <thead>
                        <tr>
                          <th>#</th>
                          <th>Description</th>
                          <th>Amount</th>
                          <th>Date</th>
                          <th>Category</th>
                          <th>Payment Method</th>
                        </tr>
                      </thead>
                      <tbody>
                        {preview.map((row) => (
                          <tr key={row._rowNumber}>
                            <td>{row._rowNumber}</td>
                            <td>{row.description}</td>
                            <td>₹{row.amount}</td>
                            <td>{row.date}</td>
                            <td>{row.category}</td>
                            <td>{row.paymentMethod}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

              {error && (
                <div className={styles['error-message']}>
                  ⚠️ {error}
                </div>
              )}
            </>
          )}
        </div>

        <div className={styles.footer}>
          {importStats ? (
            <Button onClick={handleClose} fullWidth>
              Close
            </Button>
          ) : (
            <>
              <Button
                variant="ghost"
                onClick={handleClose}
                fullWidth
              >
                Cancel
              </Button>
              <Button
                onClick={handleImport}
                disabled={!file || loading}
                loading={loading}
                fullWidth
              >
                {loading ? 'Importing...' : 'Import Expenses'}
              </Button>
            </>
          )}
        </div>
      </div>
    </div>
  );
};
