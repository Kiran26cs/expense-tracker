import { useState, useRef, useEffect } from 'react';
import styles from './DateRangePicker.module.css';

interface DateRangePickerProps {
  startDate: string;
  endDate: string;
  onDateRangeChange: (startDate: string, endDate: string) => void;
  isOpen: boolean;
  onClose: () => void;
  anchorEl?: HTMLElement | null;
}

export const DateRangePicker = ({ startDate, endDate, onDateRangeChange, isOpen, onClose, anchorEl }: DateRangePickerProps) => {
  const [currentMonth, setCurrentMonth] = useState(new Date());
  const [selectedStart, setSelectedStart] = useState<Date | null>(startDate ? new Date(startDate) : null);
  const [selectedEnd, setSelectedEnd] = useState<Date | null>(endDate ? new Date(endDate) : null);
  const [isSelectingEnd, setIsSelectingEnd] = useState(false);
  const calendarRef = useRef<HTMLDivElement>(null);
  const [position, setPosition] = useState({ top: 0, left: 0 });

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (calendarRef.current && !calendarRef.current.contains(event.target as Node)) {
        onClose();
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen, onClose]);

  useEffect(() => {
    if (isOpen && anchorEl) {
      const rect = anchorEl.getBoundingClientRect();
      const calendarWidth = 380;
      const calendarHeight = 450;
      
      let left = rect.right - calendarWidth;
      let top = rect.bottom + 8;

      // Adjust if calendar goes off screen
      if (left < 10) left = 10;
      if (left + calendarWidth > window.innerWidth - 10) {
        left = window.innerWidth - calendarWidth - 10;
      }
      if (top + calendarHeight > window.innerHeight - 10) {
        top = rect.top - calendarHeight - 8;
      }

      setPosition({ top, left });
    }
  }, [isOpen, anchorEl]);

  // Sync internal state with props when dates are cleared externally
  useEffect(() => {
    if (!startDate && !endDate) {
      setSelectedStart(null);
      setSelectedEnd(null);
      setIsSelectingEnd(false);
    }
  }, [startDate, endDate]);

  const getDaysInMonth = (date: Date) => {
    const year = date.getFullYear();
    const month = date.getMonth();
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    const daysInMonth = lastDay.getDate();
    const startingDayOfWeek = firstDay.getDay();

    return { daysInMonth, startingDayOfWeek, year, month };
  };

  const handleDateClick = (day: number) => {
    const clickedDate = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), day);
    
    if (!selectedStart || (selectedStart && selectedEnd)) {
      // Start new selection
      setSelectedStart(clickedDate);
      setSelectedEnd(null);
      setIsSelectingEnd(true);
    } else if (isSelectingEnd) {
      // Select end date
      if (clickedDate < selectedStart) {
        // If clicked date is before start, swap them
        setSelectedEnd(selectedStart);
        setSelectedStart(clickedDate);
      } else {
        setSelectedEnd(clickedDate);
      }
      setIsSelectingEnd(false);
      
      // Apply the selection
      const start = clickedDate < selectedStart ? clickedDate : selectedStart;
      const end = clickedDate < selectedStart ? selectedStart : clickedDate;
      onDateRangeChange(
        start.toISOString().split('T')[0],
        end.toISOString().split('T')[0]
      );
      
      // Auto-close after selecting end date
      setTimeout(() => onClose(), 200);
    }
  };

  const isDateInRange = (day: number) => {
    if (!selectedStart) return false;
    const date = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), day);
    
    if (selectedEnd) {
      return date >= selectedStart && date <= selectedEnd;
    } else if (isSelectingEnd) {
      return date.getTime() === selectedStart.getTime();
    }
    return false;
  };

  const isStartDate = (day: number) => {
    if (!selectedStart) return false;
    const date = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), day);
    return date.toDateString() === selectedStart.toDateString();
  };

  const isEndDate = (day: number) => {
    if (!selectedEnd) return false;
    const date = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), day);
    return date.toDateString() === selectedEnd.toDateString();
  };

  const changeMonth = (direction: number) => {
    setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() + direction, 1));
  };

  const clearSelection = () => {
    setSelectedStart(null);
    setSelectedEnd(null);
    setIsSelectingEnd(false);
    onDateRangeChange('', '');
  };

  if (!isOpen) return null;

  const { daysInMonth, startingDayOfWeek, year, month } = getDaysInMonth(currentMonth);
  const monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
  const dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  return (
    <div className={styles.overlay}>
      <div 
        className={styles.calendar} 
        ref={calendarRef}
        style={{ top: `${position.top}px`, left: `${position.left}px` }}
      >
        <div className={styles.header}>
          <button className={styles.navButton} onClick={() => changeMonth(-1)}>
            ‹
          </button>
          <div className={styles.monthYear}>
            {monthNames[month]} {year}
          </div>
          <button className={styles.navButton} onClick={() => changeMonth(1)}>
            ›
          </button>
        </div>

        <div className={styles.weekDays}>
          {dayNames.map(day => (
            <div key={day} className={styles.weekDay}>{day}</div>
          ))}
        </div>

        <div className={styles.days}>
          {Array.from({ length: startingDayOfWeek }).map((_, index) => (
            <div key={`empty-${index}`} className={styles.emptyDay}></div>
          ))}
          {Array.from({ length: daysInMonth }).map((_, index) => {
            const day = index + 1;
            const inRange = isDateInRange(day);
            const isStart = isStartDate(day);
            const isEnd = isEndDate(day);
            
            return (
              <button
                key={day}
                className={`${styles.day} ${inRange ? styles.inRange : ''} ${isStart ? styles.startDate : ''} ${isEnd ? styles.endDate : ''}`}
                onClick={() => handleDateClick(day)}
              >
                {day}
              </button>
            );
          })}
        </div>

        <div className={styles.footer}>
          <button className={styles.clearButton} onClick={clearSelection}>
            Clear
          </button>
          <button className={styles.closeButton} onClick={onClose}>
            Close
          </button>
        </div>
      </div>
    </div>
  );
};
