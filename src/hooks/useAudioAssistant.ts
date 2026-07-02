import { useState, useEffect, useCallback } from "react";

// Global state to sync audio status across components (Standby, Layout, Session)
let isAudioEnabled = false;
const listeners: Set<(enabled: boolean) => void> = new Set();

export const toggleGlobalAudio = (enabled: boolean) => {
  isAudioEnabled = enabled;
  if (!enabled) {
    window.speechSynthesis.cancel();
  }
  listeners.forEach(listener => listener(isAudioEnabled));
};

export const getGlobalAudioState = () => isAudioEnabled;

export function useAudioAssistant() {
  const [isEnabled, setIsEnabled] = useState(isAudioEnabled);

  useEffect(() => {
    const listener = (state: boolean) => setIsEnabled(state);
    listeners.add(listener);
    return () => {
      listeners.delete(listener);
    };
  }, []);

  const speak = useCallback((text: string) => {
    if (!isAudioEnabled) return;
    if (!("speechSynthesis" in window)) return;

    // Cancel any ongoing speech to prevent acoustic overlap
    window.speechSynthesis.cancel();

    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = "de-DE";
    utterance.rate = 0.95; // Slightly slower for elderly demographic
    utterance.pitch = 1.0;

    // Try to find a good German voice, preferably Google or native
    const voices = window.speechSynthesis.getVoices();
    const deVoice = voices.find(v => v.lang === "de-DE" && v.name.includes("Google")) || 
                    voices.find(v => v.lang.startsWith("de"));
                    
    if (deVoice) {
      utterance.voice = deVoice;
    }

    window.speechSynthesis.speak(utterance);
  }, []);

  const toggleAudio = useCallback(() => {
    toggleGlobalAudio(!isAudioEnabled);
  }, []);

  return { isEnabled, speak, toggleAudio };
}
