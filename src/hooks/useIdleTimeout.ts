import { useState, useEffect, useRef, useCallback } from 'react';

interface IdleTimeoutOptions {
  idleTimeMs: number; // Time before warning (e.g. 180000 = 3 mins)
  warningTimeMs: number; // Time in warning state before reset (e.g. 30000 = 30s)
  onIdleCallback: () => void; // Triggered when fully idle (to reset state)
}

export function useIdleTimeout({ idleTimeMs, warningTimeMs, onIdleCallback }: IdleTimeoutOptions) {
  const [isWarning, setIsWarning] = useState(false);
  const [remainingTime, setRemainingTime] = useState(warningTimeMs / 1000);
  const idleTimerRef = useRef<NodeJS.Timeout | null>(null);
  const warningTimerRef = useRef<NodeJS.Timeout | null>(null);
  const countdownIntervalRef = useRef<NodeJS.Timeout | null>(null);

  const clearAllTimers = useCallback(() => {
    if (idleTimerRef.current) clearTimeout(idleTimerRef.current);
    if (warningTimerRef.current) clearTimeout(warningTimerRef.current);
    if (countdownIntervalRef.current) clearInterval(countdownIntervalRef.current);
  }, []);

  const triggerWarning = useCallback(() => {
    setIsWarning(true);
    setRemainingTime(warningTimeMs / 1000);
    
    // Start countdown for UI
    countdownIntervalRef.current = setInterval(() => {
      setRemainingTime((prev) => {
        if (prev <= 1) {
          clearInterval(countdownIntervalRef.current!);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    // Start final warning timer
    warningTimerRef.current = setTimeout(() => {
      clearAllTimers();
      setIsWarning(false);
      onIdleCallback(); // Execute the reset
    }, warningTimeMs);
  }, [warningTimeMs, clearAllTimers, onIdleCallback]);

  const resetTimer = useCallback(() => {
    // If in warning state, any user action cancels the warning and restarts idle
    if (isWarning) {
      setIsWarning(false);
    }
    clearAllTimers();

    // Start the main idle timer
    idleTimerRef.current = setTimeout(triggerWarning, idleTimeMs);
  }, [idleTimeMs, isWarning, clearAllTimers, triggerWarning]);

  useEffect(() => {
    const events = ['mousemove', 'keydown', 'wheel', 'DOMMouseScroll', 'mousewheel', 'mousedown', 'touchstart', 'touchmove', 'MSPointerDown', 'MSPointerMove'];
    
    const eventHandler = () => resetTimer();
    
    events.forEach(event => {
      window.addEventListener(event, eventHandler);
    });

    // Start timer initially
    resetTimer();

    return () => {
      events.forEach(event => {
        window.removeEventListener(event, eventHandler);
      });
      clearAllTimers();
    };
  }, [resetTimer, clearAllTimers]);

  return { isWarning, remainingTime, resetTimer };
}
