import { useState, useEffect, useRef, useCallback } from 'react';
import { Modal } from '@/components/Modal/Modal';
import { Button } from '@/components/Button/Button';
import { Input } from '@/components/Input/Input';
import { settingsApi } from '@/services/settings.api';
import type { Category } from '@/types';
import styles from './CategoryManagementModal.module.css';

interface CategoryManagementModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess?: () => void;
  expenseBookId: string;
}

// Font Awesome icon options for categories
const FA_ICONS = [
  { icon: 'fa-solid fa-utensils', label: 'Food' },
  { icon: 'fa-solid fa-car', label: 'Transport' },
  { icon: 'fa-solid fa-bag-shopping', label: 'Shopping' },
  { icon: 'fa-solid fa-bolt', label: 'Utilities' },
  { icon: 'fa-solid fa-film', label: 'Entertainment' },
  { icon: 'fa-solid fa-heart-pulse', label: 'Health' },
  { icon: 'fa-solid fa-graduation-cap', label: 'Education' },
  { icon: 'fa-solid fa-house', label: 'Rent' },
  { icon: 'fa-solid fa-cart-shopping', label: 'Groceries' },
  { icon: 'fa-solid fa-plane', label: 'Travel' },
  { icon: 'fa-solid fa-gamepad', label: 'Gaming' },
  { icon: 'fa-solid fa-shirt', label: 'Clothing' },
  { icon: 'fa-solid fa-mobile-screen', label: 'Phone' },
  { icon: 'fa-solid fa-dumbbell', label: 'Fitness' },
  { icon: 'fa-solid fa-gift', label: 'Gifts' },
  { icon: 'fa-solid fa-baby', label: 'Kids' },
  { icon: 'fa-solid fa-paw', label: 'Pets' },
  { icon: 'fa-solid fa-briefcase', label: 'Work' },
  { icon: 'fa-solid fa-piggy-bank', label: 'Savings' },
  { icon: 'fa-solid fa-ellipsis', label: 'Other' },
];

const COLOR_OPTIONS = [
  '#ef4444', '#f97316', '#f59e0b', '#22c55e',
  '#10b981', '#14b8a6', '#3b82f6', '#6366f1',
  '#8b5cf6', '#ec4899', '#64748b', '#0ea5e9',
];

export const CategoryManagementModal = ({ isOpen, onClose, onSuccess, expenseBookId }: CategoryManagementModalProps) => {
  const [view, setView] = useState<'main' | 'add'>('main');
  const [activeTab, setActiveTab] = useState<'manual' | 'upload'>('manual');
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [successMsg, setSuccessMsg] = useState('');

  // Add category form state
  const [newCategory, setNewCategory] = useState({
    name: '',
    icon: 'fa-solid fa-tag',
    color: '#6366f1'
  });

  // Edit state
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editData, setEditData] = useState({ name: '', icon: '', color: '' });

  // Import state
  const [importFile, setImportFile] = useState<File | null>(null);
  const [importData, setImportData] = useState<Array<{ name: string; icon: string; color: string }>>([]);
  const [isDragging, setIsDragging] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (isOpen) {
      fetchCategories();
    }
  }, [isOpen]);

  useEffect(() => {
    if (successMsg) {
      const timer = setTimeout(() => setSuccessMsg(''), 3000);
      return () => clearTimeout(timer);
    }
  }, [successMsg]);

  const fetchCategories = async () => {
    try {
      setLoading(true);
      const response = await settingsApi.getCategories();
      if (response.success && response.data) {
        setCategories(response.data);
      }
    } catch {
      // Silently fail
    } finally {
      setLoading(false);
    }
  };

  const handleAddCategory = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!newCategory.name.trim()) {
      setError('Category name is required');
      return;
    }

    try {
      setLoading(true);
      const response = await settingsApi.createCategory(newCategory);

      if (response.success) {
        setNewCategory({ name: '', icon: 'fa-solid fa-tag', color: '#6366f1' });
        setSuccessMsg('Category added successfully!');
        setView('main');
        await fetchCategories();
        onSuccess?.();
      } else {
        setError(response.error || 'Failed to create category');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create category');
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateCategory = async () => {
    if (!editingId) return;
    setError('');

    try {
      setLoading(true);
      const response = await settingsApi.updateCategory(editingId, editData);
      if (response.success) {
        setEditingId(null);
        setSuccessMsg('Category updated!');
        await fetchCategories();
        onSuccess?.();
      } else {
        setError(response.error || 'Failed to update');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update');
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteCategory = async (id: string, name: string) => {
    if (!confirm(`Delete category "${name}"?`)) return;

    try {
      setLoading(true);
      const response = await settingsApi.deleteCategory(id);
      if (response.success) {
        setSuccessMsg('Category deleted');
        await fetchCategories();
        onSuccess?.();
      } else {
        setError(response.error || 'Failed to delete');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Cannot delete this category');
    } finally {
      setLoading(false);
    }
  };

  const startEdit = (cat: Category) => {
    setEditingId(cat.id);
    setEditData({ name: cat.name, icon: cat.icon, color: cat.color });
  };

  // CSV handling
  const parseCSV = useCallback((text: string) => {
    const lines = text.trim().split('\n');
    if (lines.length < 2) {
      setError('CSV file must have a header row and at least one data row');
      return;
    }

    const header = lines[0].toLowerCase();
    if (!header.includes('name')) {
      setError('CSV must have a "name" column');
      return;
    }

    const dataLines = lines.slice(1);
    const parsed: Array<{ name: string; icon: string; color: string }> = [];

    for (const line of dataLines) {
      const parts = line.split(',').map(s => s.trim().replace(/^"|"$/g, ''));
      const [name, icon, color] = parts;
      if (name) {
        parsed.push({
          name,
          icon: icon || 'fa-solid fa-tag',
          color: color || '#6366f1'
        });
      }
    }

    if (parsed.length === 0) {
      setError('No valid data found in CSV');
      return;
    }

    setImportData(parsed);
    setError('');
  }, []);

  const handleFileSelect = (file: File) => {
    if (!file.name.endsWith('.csv')) {
      setError('Please upload a CSV file');
      return;
    }
    setImportFile(file);
    const reader = new FileReader();
    reader.onload = (e) => parseCSV(e.target?.result as string);
    reader.readAsText(file);
  };

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) handleFileSelect(file);
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    const file = e.dataTransfer.files?.[0];
    if (file) handleFileSelect(file);
  };

  const handleImport = async () => {
    if (importData.length === 0) {
      setError('No data to import');
      return;
    }

    try {
      setLoading(true);
      const response = await settingsApi.importCategories(
        importData.map(d => ({ name: d.name, icon: d.icon, color: d.color }))
      );

      if (response.success && response.data) {
        setSuccessMsg(
          `Imported ${response.data.imported} categories` +
          (response.data.failed > 0 ? `, ${response.data.failed} failed` : '')
        );
        setImportFile(null);
        setImportData([]);
        setActiveTab('manual');
        setView('main');
        await fetchCategories();
        onSuccess?.();
      } else {
        setError(response.error || 'Import failed');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Import failed');
    } finally {
      setLoading(false);
    }
  };

  const downloadTemplate = () => {
    const csv = 'name,icon,color\nFood & Dining,fa-solid fa-utensils,#ef4444\nTransport,fa-solid fa-car,#3b82f6\nShopping,fa-solid fa-bag-shopping,#8b5cf6';
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'categories_template.csv';
    a.click();
    URL.revokeObjectURL(url);
  };

  const handleClose = () => {
    setView('main');
    setActiveTab('manual');
    setError('');
    setSuccessMsg('');
    setNewCategory({ name: '', icon: 'fa-solid fa-tag', color: '#6366f1' });
    setImportFile(null);
    setImportData([]);
    setEditingId(null);
    onClose();
  };

  /* ─── MAIN VIEW ─── */
  const renderMainView = () => (
    <div className={styles.mainView}>
      {error && <div className={styles.error}><i className="fa-solid fa-circle-exclamation" /> {error}</div>}
      {successMsg && <div className={styles.success}><i className="fa-solid fa-circle-check" /> {successMsg}</div>}

      <div className={styles.actions}>
        <button className={styles.actionCard} onClick={() => { setView('add'); setActiveTab('manual'); }}>
          <i className="fa-solid fa-plus" />
          <span>Add Category</span>
        </button>
        <button className={styles.actionCard} onClick={() => { setView('add'); setActiveTab('upload'); }}>
          <i className="fa-solid fa-file-import" />
          <span>Import Categories</span>
        </button>
      </div>

      <div className={styles.categoriesList}>
        <div className={styles.sectionHeader}>
          <h3 className={styles.sectionTitle}>All Categories</h3>
          <span className={styles.badge}>{categories.length}</span>
        </div>

        {loading ? (
          <div className={styles.loading}><i className="fa-solid fa-spinner fa-spin" /> Loading...</div>
        ) : categories.length === 0 ? (
          <div className={styles.empty}>
            <i className="fa-solid fa-folder-open" style={{ fontSize: '2rem', marginBottom: '0.5rem' }} />
            <p>No categories yet</p>
            <p style={{ fontSize: '0.8rem' }}>Click "Add Category" to get started</p>
          </div>
        ) : (
          <div className={styles.categoryGrid}>
            {categories.map((cat) => (
              <div key={cat.id} className={styles.categoryCard}>
                {editingId === cat.id ? (
                  <div className={styles.editRow}>
                    <input
                      className={styles.editInput}
                      value={editData.name}
                      onChange={(e) => setEditData({ ...editData, name: e.target.value })}
                      autoFocus
                    />
                    <button className={styles.iconBtn} onClick={handleUpdateCategory} title="Save">
                      <i className="fa-solid fa-check" style={{ color: 'var(--color-success)' }} />
                    </button>
                    <button className={styles.iconBtn} onClick={() => setEditingId(null)} title="Cancel">
                      <i className="fa-solid fa-xmark" style={{ color: 'var(--color-danger)' }} />
                    </button>
                  </div>
                ) : (
                  <>
                    <div className={styles.categoryInfo}>
                      <span className={styles.categoryIcon} style={{ color: cat.color }}>
                        <i className={cat.icon || 'fa-solid fa-tag'} />
                      </span>
                      <span className={styles.categoryName}>{cat.name}</span>
                    </div>
                    <div className={styles.categoryActions}>
                      <button className={styles.iconBtn} onClick={() => startEdit(cat)} title="Edit">
                        <i className="fa-solid fa-pen" />
                      </button>
                      <button className={styles.iconBtn} onClick={() => handleDeleteCategory(cat.id, cat.name)} title="Delete">
                        <i className="fa-solid fa-trash" />
                      </button>
                    </div>
                  </>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );

  /* ─── ADD / IMPORT VIEW (Tabbed) ─── */
  const renderAddView = () => (
    <div className={styles.addView}>
      {error && <div className={styles.error}><i className="fa-solid fa-circle-exclamation" /> {error}</div>}
      {successMsg && <div className={styles.success}><i className="fa-solid fa-circle-check" /> {successMsg}</div>}

      <div className={styles.tabs}>
        <button
          className={`${styles.tab} ${activeTab === 'manual' ? styles.activeTab : ''}`}
          onClick={() => setActiveTab('manual')}
        >
          Manual
        </button>
        <button
          className={`${styles.tab} ${activeTab === 'upload' ? styles.activeTab : ''}`}
          onClick={() => setActiveTab('upload')}
        >
          Upload
        </button>
      </div>

      {activeTab === 'manual' ? renderManualTab() : renderUploadTab()}
    </div>
  );

  /* ─── MANUAL TAB ─── */
  const renderManualTab = () => (
    <form onSubmit={handleAddCategory} className={styles.manualForm}>
      <Input
        label="Category Name"
        type="text"
        value={newCategory.name}
        onChange={(e) => setNewCategory({ ...newCategory, name: e.target.value })}
        placeholder="e.g., Food, Transport, Entertainment"
        required
      />

      <div className={styles.formGroup}>
        <label className={styles.label}>Select Icon</label>
        <div className={styles.iconGrid}>
          {FA_ICONS.map(({ icon, label }) => (
            <button
              key={icon}
              type="button"
              className={`${styles.iconOption} ${newCategory.icon === icon ? styles.selected : ''}`}
              onClick={() => setNewCategory({ ...newCategory, icon })}
              title={label}
            >
              <i className={icon} />
            </button>
          ))}
        </div>
      </div>

      <div className={styles.formGroup}>
        <label className={styles.label}>Select Color</label>
        <div className={styles.colorGrid}>
          {COLOR_OPTIONS.map((color) => (
            <button
              key={color}
              type="button"
              className={`${styles.colorOption} ${newCategory.color === color ? styles.selected : ''}`}
              style={{ backgroundColor: color }}
              onClick={() => setNewCategory({ ...newCategory, color })}
            />
          ))}
        </div>
      </div>

      {/* Preview */}
      <div className={styles.previewBox}>
        <span className={styles.previewLabel}>Preview</span>
        <div className={styles.previewCategory}>
          <span style={{ color: newCategory.color, fontSize: '1.5rem' }}>
            <i className={newCategory.icon} />
          </span>
          <span style={{ fontWeight: 600 }}>{newCategory.name || 'Category Name'}</span>
          <span className={styles.previewDot} style={{ backgroundColor: newCategory.color }} />
        </div>
      </div>

      <div className={styles.formFooter}>
        <Button type="button" variant="ghost" onClick={() => setView('main')} disabled={loading}>
          <i className="fa-solid fa-arrow-left" /> Back
        </Button>
        <Button type="submit" disabled={loading || !newCategory.name.trim()}>
          {loading ? <><i className="fa-solid fa-spinner fa-spin" /> Adding...</> : 'SUBMIT'}
        </Button>
      </div>
    </form>
  );

  /* ─── UPLOAD TAB ─── */
  const renderUploadTab = () => (
    <div className={styles.uploadTab}>
      <div className={styles.uploadHeader}>
        <div>
          <h4>Upload a CSV</h4>
          <p className={styles.uploadSubtext}>
            Upload a .csv file with the following sample information
          </p>
          <p className={styles.uploadSubtext}>
            Columns: <strong>name</strong>, <strong>icon</strong> (Font Awesome class), <strong>color</strong> (hex)
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
              <th>name</th>
              <th>icon</th>
              <th>color</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>Food & Dining</td>
              <td>fa-solid fa-utensils</td>
              <td>#ef4444</td>
            </tr>
            <tr>
              <td>Transport</td>
              <td>fa-solid fa-car</td>
              <td>#3b82f6</td>
            </tr>
          </tbody>
        </table>
      </div>

      {/* Drag & Drop Zone */}
      <div
        className={`${styles.dropZone} ${isDragging ? styles.dropZoneActive : ''} ${importFile ? styles.dropZoneHasFile : ''}`}
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
        {importFile ? (
          <div className={styles.fileInfo}>
            <i className="fa-solid fa-file-csv" style={{ fontSize: '2rem', color: 'var(--color-success)' }} />
            <span className={styles.fileName}>{importFile.name}</span>
            <span className={styles.fileSize}>{(importFile.size / 1024).toFixed(1)} KB</span>
            <button
              className={styles.removeFile}
              onClick={(e) => { e.stopPropagation(); setImportFile(null); setImportData([]); }}
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

      {/* Import Preview Table */}
      {importData.length > 0 && (
        <div className={styles.importPreview}>
          <h4>Preview ({importData.length} categories)</h4>
          <div className={styles.previewTableWrap}>
            <table className={styles.previewTable}>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Icon</th>
                  <th>Name</th>
                  <th>Color</th>
                </tr>
              </thead>
              <tbody>
                {importData.map((cat, idx) => (
                  <tr key={idx}>
                    <td>{idx + 1}</td>
                    <td><i className={cat.icon} style={{ color: cat.color }} /></td>
                    <td>{cat.name}</td>
                    <td>
                      <span className={styles.colorSwatch} style={{ backgroundColor: cat.color }} />
                      {cat.color}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <div className={styles.formFooter}>
        <Button type="button" variant="ghost" onClick={() => setView('main')} disabled={loading}>
          <i className="fa-solid fa-arrow-left" /> Back
        </Button>
        <Button onClick={handleImport} disabled={loading || importData.length === 0}>
          {loading ? <><i className="fa-solid fa-spinner fa-spin" /> Importing...</> : 'SUBMIT'}
        </Button>
      </div>
    </div>
  );

  const getTitle = () => {
    if (view === 'add') return 'Add Expense Category';
    return 'Manage Categories';
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title={getTitle()} size="large">
      {view === 'main' && renderMainView()}
      {view === 'add' && renderAddView()}
    </Modal>
  );
};