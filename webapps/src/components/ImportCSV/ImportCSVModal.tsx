import { useState, useRef, useCallback } from 'react';
import { Modal } from '@/components/Modal/Modal';
import { Button } from '@/components/Button/Button';
import { Input } from '@/components/Input/Input';
import { Select } from '@/components/Select/Select';
import { expenseApi } from '@/services/expense.api';
import styles from './ImportCSVModal.module.css';

interface ImportCSVModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (count: number) => void;
}

export const ImportCSVModal = ({ isOpen, onClose, onSuccess }: ImportCSVModalProps) => {
  const [activeTab, setActiveTab] = useState<'manual' | 'upload'>('manual');

  // ── Manual form state ──
  const [amount, setAmount] = useState('');
  const [date, setDate] = useState(new Date().toISOString().split('T')[0]);
  const [category, setCategory] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('');
  const [description, setDescription] = useState('');
  const [notes, setNotes] = useState('');
  const [isRecurring, setIsRecurring] = useState(false);
  const [frequency, setFrequency] = useState('');

  // ── Upload state ──
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [preview, setPreview] = useState<any[]>([]);
  const [importStats, setImportStats] = useState<{ success: number; failed: number } | null>(null);

  // ── Shared state ──
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [successMsg, setSuccessMsg] = useState('');

  // ──────────────── Manual – Submit ────────────────
  const handleManualSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!amount || !date || !category || !paymentMethod || !description) {
      setError('Please fill in all required fields');
      return;
    }

    try {
      setLoading(true);
      const expenseData: any = {
        amount: parseFloat(amount),
        date: new Date(date).toISOString(),
        category,
        paymentMethod,
        description,
        notes: notes || undefined,
        isRecurring,
      };
      if (isRecurring && frequency) {
        expenseData.recurringConfig = {
          frequency,
          startDate: new Date(date).toISOString(),
          endDate: null,
        };
      }

      const response = await expenseApi.createExpense(expenseData);
      if (response.success) {
        resetManualForm();
        setSuccessMsg('Expense added successfully!');
        onSuccess(1);
        setTimeout(() => handleClose(), 1200);
      } else {
        setError(response.error || 'Failed to create expense');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create expense');
    } finally {
      setLoading(false);
    }
  };

  const resetManualForm = () => {
    setAmount('');
    setDate(new Date().toISOString().split('T')[0]);
    setCategory('');
    setPaymentMethod('');
    setDescription('');
    setNotes('');
    setIsRecurring(false);
    setFrequency('');
  };

  // ──────────────── Upload – CSV handling ────────────────
  const parseCSV = useCallback((text: string) => {
    const lines = text.trim().split('\n');
    if (lines.length < 2) {
      setError('CSV file must have a header row and at least one data row');
      return;
    }

    const headers = lines[0].split(',').map(h => h.trim().toLowerCase());
    const requiredHeaders = ['description', 'amount', 'date', 'category', 'paymentmethod'];
    const missing = requiredHeaders.filter(h => !headers.includes(h));

    if (missing.length > 0) {
      setError(`Missing required columns: ${missing.join(', ')}`);
      return;
    }

    const rows = lines.slice(1).map((line, idx) => {
      const values = line.split(',').map(v => v.trim().replace(/^"|"$/g, ''));
      const row: any = {};
      headers.forEach((header, i) => {
        row[header] = values[i] || '';
      });
      row._rowNumber = idx + 2;
      return row;
    }).filter(r => r.description && r.amount);

    if (rows.length === 0) {
      setError('No valid data found in CSV');
      return;
    }

    setPreview(rows);
    setError('');
  }, []);

  const handleFileSelect = (selectedFile: File) => {
    if (!selectedFile.name.endsWith('.csv')) {
      setError('Please upload a CSV file');
      return;
    }
    setFile(selectedFile);
    setError('');
    setPreview([]);
    const reader = new FileReader();
    reader.onload = (e) => parseCSV(e.target?.result as string);
    reader.readAsText(selectedFile);
  };

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0];
    if (f) handleFileSelect(f);
  };

  const handleDragOver = (e: React.DragEvent) => { e.preventDefault(); setIsDragging(true); };
  const handleDragLeave = (e: React.DragEvent) => { e.preventDefault(); setIsDragging(false); };
  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    const f = e.dataTransfer.files?.[0];
    if (f) handleFileSelect(f);
  };

  const downloadTemplate = () => {
    const csv = `description,amount,date,category,paymentMethod,notes\nGrocery Shopping,500,2026-02-03,food,cash,Weekly shopping\nGas Refill,1000,2026-02-02,transport,card,Monthly fuel\nMovie Tickets,400,2026-02-01,entertainment,upi,Movie night`;
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'expense_template.csv';
    a.click();
    URL.revokeObjectURL(url);
  };

  const handleImport = async () => {
    if (!file || preview.length === 0) { setError('No data to import'); return; }

    setLoading(true);
    setError('');

    try {
      let successCount = 0;
      let failedCount = 0;

      for (const row of preview) {
        try {
          const amt = parseFloat(row.amount);
          const dateObj = new Date(row.date);
          if (isNaN(amt) || amt <= 0 || isNaN(dateObj.getTime())) { failedCount++; continue; }

          const response = await expenseApi.createExpense({
            description: row.description,
            amount: amt,
            date: dateObj.toISOString(),
            category: row.category?.toLowerCase(),
            paymentMethod: (row.paymentmethod || row.paymentMethod || '').toLowerCase(),
            notes: row.notes || undefined,
            isRecurring: false,
          });
          if (response.success) successCount++; else failedCount++;
        } catch { failedCount++; }
      }

      setImportStats({ success: successCount, failed: failedCount });
      if (successCount > 0) {
        setTimeout(() => onSuccess(successCount), 1500);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Import failed');
    } finally {
      setLoading(false);
    }
  };

  // ──────────────── Close / Reset ────────────────
  const handleClose = () => {
    resetManualForm();
    setFile(null);
    setPreview([]);
    setImportStats(null);
    setError('');
    setSuccessMsg('');
    setActiveTab('manual');
    onClose();
  };

  // ──────────────── RENDER ────────────────
  return (
    <Modal isOpen={isOpen} onClose={handleClose} title="Add Expense" size="large">
      {error && <div className={styles.error}><i className="fa-solid fa-circle-exclamation" /> {error}</div>}
      {successMsg && <div className={styles.success}><i className="fa-solid fa-circle-check" /> {successMsg}</div>}

      {/* ── Import Result Screen ── */}
      {importStats ? (
        <div className={styles.importResult}>
          <i className="fa-solid fa-circle-check" style={{ fontSize: '3rem', color: 'var(--color-success)' }} />
          <h3>Import Complete!</h3>
          <div className={styles.statRow}>
            <span className={styles.statLabel}>Successfully imported:</span>
            <span className={styles.statSuccess}>{importStats.success} expenses</span>
          </div>
          {importStats.failed > 0 && (
            <div className={styles.statRow}>
              <span className={styles.statLabel}>Failed:</span>
              <span className={styles.statFailed}>{importStats.failed} rows</span>
            </div>
          )}
          <p className={styles.statInfo}>Your expenses will appear in the list shortly.</p>
          <Button onClick={handleClose} style={{ marginTop: '1rem' }}>Close</Button>
        </div>
      ) : (
        <>
          {/* ── Tabs ── */}
          <div className={styles.tabs}>
            <button
              className={`${styles.tab} ${activeTab === 'manual' ? styles.activeTab : ''}`}
              onClick={() => setActiveTab('manual')}
            >
              <i className="fa-solid fa-pen" /> Manual
            </button>
            <button
              className={`${styles.tab} ${activeTab === 'upload' ? styles.activeTab : ''}`}
              onClick={() => setActiveTab('upload')}
            >
              <i className="fa-solid fa-cloud-arrow-up" /> Upload
            </button>
          </div>

          {activeTab === 'manual' ? (
            /* ═══════ MANUAL TAB ═══════ */
            <form onSubmit={handleManualSubmit} className={styles.manualForm}>
              <div className={styles.formRow}>
                <Input
                  label="Amount"
                  type="number"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  placeholder="0.00"
                  required
                />
                <Input
                  label="Date"
                  type="date"
                  value={date}
                  onChange={(e) => setDate(e.target.value)}
                  required
                />
              </div>

              <div className={styles.formRow}>
                <Select
                  label="Category"
                  options={[
                    { value: 'food', label: 'Food & Dining' },
                    { value: 'transport', label: 'Transportation' },
                    { value: 'shopping', label: 'Shopping' },
                    { value: 'bills', label: 'Bills & Utilities' },
                    { value: 'entertainment', label: 'Entertainment' },
                    { value: 'health', label: 'Health' },
                    { value: 'education', label: 'Education' },
                    { value: 'rent', label: 'Rent' },
                    { value: 'groceries', label: 'Groceries' },
                    { value: 'other', label: 'Other' },
                  ]}
                  value={category}
                  onChange={(e) => setCategory(e.target.value)}
                  required
                />
                <Select
                  label="Payment Method"
                  options={[
                    { value: 'cash', label: 'Cash' },
                    { value: 'card', label: 'Credit/Debit Card' },
                    { value: 'upi', label: 'UPI' },
                    { value: 'bank', label: 'Bank Transfer' },
                  ]}
                  value={paymentMethod}
                  onChange={(e) => setPaymentMethod(e.target.value)}
                  required
                />
              </div>

              <Input
                label="Description"
                type="text"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Brief description of the expense"
                required
              />

              <Input
                label="Notes (optional)"
                type="text"
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                placeholder="Additional notes"
              />

              <div className={styles.checkboxRow}>
                <label className={styles.checkbox}>
                  <input
                    type="checkbox"
                    checked={isRecurring}
                    onChange={(e) => setIsRecurring(e.target.checked)}
                  />
                  <span>This is a recurring expense</span>
                </label>
              </div>

              {isRecurring && (
                <Select
                  label="Frequency"
                  options={[
                    { value: 'daily', label: 'Daily' },
                    { value: 'weekly', label: 'Weekly' },
                    { value: 'monthly', label: 'Monthly' },
                    { value: 'yearly', label: 'Yearly' },
                  ]}
                  value={frequency}
                  onChange={(e) => setFrequency(e.target.value)}
                  required={isRecurring}
                />
              )}

              <div className={styles.formFooter}>
                <Button type="button" variant="ghost" onClick={handleClose} disabled={loading}>
                  Cancel
                </Button>
                <Button type="submit" disabled={loading}>
                  {loading ? <><i className="fa-solid fa-spinner fa-spin" /> Saving...</> : 'SUBMIT'}
                </Button>
              </div>
            </form>
          ) : (
            /* ═══════ UPLOAD TAB ═══════ */
            <div className={styles.uploadTab}>
              <div className={styles.uploadHeader}>
                <div>
                  <h4>Upload a CSV</h4>
                  <p className={styles.uploadSubtext}>
                    Upload a .csv file with the following sample information
                  </p>
                  <p className={styles.uploadSubtext}>
                    Columns: <strong>description</strong>, <strong>amount</strong>, <strong>date</strong>, <strong>category</strong>, <strong>paymentMethod</strong>, <strong>notes</strong>
                  </p>
                </div>
                <button className={styles.downloadTemplate} onClick={downloadTemplate}>
                  <i className="fa-solid fa-download" /> Download Template
                </button>
              </div>

              {/* Sample table */}
              <div className={styles.sampleTable}>
                <table>
                  <thead>
                    <tr>
                      <th>description</th>
                      <th>amount</th>
                      <th>date</th>
                      <th>category</th>
                      <th>paymentMethod</th>
                      <th>notes</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td>Grocery Shopping</td>
                      <td>500</td>
                      <td>2026-02-03</td>
                      <td>food</td>
                      <td>cash</td>
                      <td>Weekly shopping</td>
                    </tr>
                    <tr>
                      <td>Gas Refill</td>
                      <td>1000</td>
                      <td>2026-02-02</td>
                      <td>transport</td>
                      <td>card</td>
                      <td>Monthly fuel</td>
                    </tr>
                  </tbody>
                </table>
              </div>

              {/* Drag & Drop Zone */}
              <div
                className={`${styles.dropZone} ${isDragging ? styles.dropZoneActive : ''} ${file ? styles.dropZoneHasFile : ''}`}
                onDragOver={handleDragOver}
                onDragLeave={handleDragLeave}
                onDrop={handleDrop}
                onClick={() => fileInputRef.current?.click()}
              >
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".csv"
                  onChange={handleFileInput}
                  className={styles.hiddenInput}
                />
                {file ? (
                  <div className={styles.fileInfo}>
                    <i className="fa-solid fa-file-csv" style={{ fontSize: '2rem', color: 'var(--color-success)' }} />
                    <span className={styles.fileName}>{file.name}</span>
                    <span className={styles.fileSize}>{(file.size / 1024).toFixed(1)} KB</span>
                    <button
                      className={styles.removeFile}
                      onClick={(e) => { e.stopPropagation(); setFile(null); setPreview([]); }}
                    >
                      <i className="fa-solid fa-xmark" />
                    </button>
                  </div>
                ) : (
                  <div className={styles.dropContent}>
                    <i className="fa-solid fa-cloud-arrow-up" style={{ fontSize: '2.5rem', color: 'var(--color-primary)' }} />
                    <p>Drag & Drop your CSV File or <span className={styles.browseLink}>browse</span></p>
                  </div>
                )}
              </div>

              {/* Preview Table */}
              {preview.length > 0 && (
                <div className={styles.importPreview}>
                  <h4>Preview ({preview.length} expenses)</h4>
                  <div className={styles.previewTableWrap}>
                    <table className={styles.previewTable}>
                      <thead>
                        <tr>
                          <th>#</th>
                          <th>Description</th>
                          <th>Amount</th>
                          <th>Date</th>
                          <th>Category</th>
                          <th>Payment</th>
                        </tr>
                      </thead>
                      <tbody>
                        {preview.slice(0, 10).map((row) => (
                          <tr key={row._rowNumber}>
                            <td>{row._rowNumber}</td>
                            <td>{row.description}</td>
                            <td>₹{row.amount}</td>
                            <td>{row.date}</td>
                            <td>{row.category}</td>
                            <td>{row.paymentmethod || row.paymentMethod}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                    {preview.length > 10 && (
                      <div className={styles.previewMore}>...and {preview.length - 10} more rows</div>
                    )}
                  </div>
                </div>
              )}

              <div className={styles.formFooter}>
                <Button type="button" variant="ghost" onClick={handleClose} disabled={loading}>
                  Cancel
                </Button>
                <Button onClick={handleImport} disabled={loading || preview.length === 0}>
                  {loading ? <><i className="fa-solid fa-spinner fa-spin" /> Importing...</> : 'SUBMIT'}
                </Button>
              </div>
            </div>
          )}
        </>
      )}
    </Modal>
  );
};