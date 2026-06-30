import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { initGoogleAnalytics } from '@/src/lib/analytics';

const CONSENT_KEY = 'atm_cookie_consent';

export default function CookieBanner() {
  const [showBanner, setShowBanner] = useState(false);

  useEffect(() => {
    // Check if consent has already been given or denied
    const consent = localStorage.getItem(CONSENT_KEY);
    
    if (!consent) {
      // Show banner if no decision has been made
      setShowBanner(true);
    } else if (consent === 'granted') {
      // Initialize GA immediately if consent was previously granted
      initGoogleAnalytics();
    }
  }, []);

  const handleAccept = () => {
    localStorage.setItem(CONSENT_KEY, 'granted');
    initGoogleAnalytics();
    setShowBanner(false);
  };

  const handleDecline = () => {
    localStorage.setItem(CONSENT_KEY, 'denied');
    setShowBanner(false);
  };

  if (!showBanner) return null;

  return (
    <div className="fixed bottom-0 left-0 right-0 bg-slate-900 text-slate-200 p-6 z-50 shadow-2xl border-t border-slate-800 animate-in slide-in-from-bottom-full duration-500">
      <div className="max-w-7xl mx-auto flex flex-col md:flex-row items-center justify-between gap-6">
        <div className="flex-1">
          <h3 className="text-lg font-bold text-white mb-2">Ihre Privatsphäre ist uns wichtig</h3>
          <p className="text-sm text-slate-400">
            Wir verwenden Cookies und Analyse-Tools (Google Analytics), um unsere B2B-Plattform kontinuierlich zu verbessern und Ihnen das bestmögliche Erlebnis zu bieten. 
            Diese Daten werden ausschließlich zur Optimierung der Benutzerführung erhoben. Sie können Ihre Zustimmung jederzeit widerrufen.
          </p>
        </div>
        <div className="flex flex-col sm:flex-row gap-4 shrink-0">
          <Button 
            variant="outline" 
            onClick={handleDecline}
            className="border-slate-700 text-slate-300 hover:bg-slate-800 hover:text-white"
          >
            Nur essenzielle Cookies
          </Button>
          <Button 
            onClick={handleAccept}
            className="bg-[#0082C8] hover:bg-[#006A9C] text-white font-semibold px-8"
          >
            Alle akzeptieren
          </Button>
        </div>
      </div>
    </div>
  );
}
