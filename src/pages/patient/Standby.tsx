import { useNavigate, useSearchParams } from 'react-router-dom';
import { Smartphone } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { toggleGlobalAudio } from '@/src/hooks/useAudioAssistant';

export default function Standby() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const startSession = () => {
    // Enable audio mode automatically (Web Speech API requires user gesture)
    toggleGlobalAudio(true);
    
    // Fire a silent utterance to fully initialize the speech engine on iOS/Android/Chrome
    if ('speechSynthesis' in window) {
      const initUtterance = new SpeechSynthesisUtterance('');
      window.speechSynthesis.speak(initUtterance);
    }

    // Retrieve pharmacy_id from URL and store it
    const pharmacyId = searchParams.get('pharmacy_id');
    if (pharmacyId) {
      sessionStorage.setItem('byod_pharmacy_id', pharmacyId);
    }
    
    // Generate a new session ID
    const sessionId = crypto.randomUUID();
    
    // Log telemetry
    const storedPharmacyId = pharmacyId || sessionStorage.getItem('byod_pharmacy_id');
    if (storedPharmacyId) {
      fetch("/api/kiosk/telemetry", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ 
          session_id: sessionId, 
          event_type: "SESSION_STARTED",
          pharmacy_id: storedPharmacyId 
        })
      }).catch(console.error);
    }

    navigate(`/patient/session/${sessionId}`);
  };

  return (
    <div className="flex-1 flex flex-col items-center justify-center p-6 text-center bg-white min-h-[100dvh]">
      <div className="w-full max-w-md mx-auto space-y-8">
        <div className="w-16 h-16 bg-red-50 text-red-600 rounded-2xl flex items-center justify-center mx-auto mb-2 shadow-sm border border-red-100">
          <Smartphone className="w-8 h-8" strokeWidth={2.5} />
        </div>
        
        <div className="space-y-3">
          <h1 className="text-3xl sm:text-4xl font-extrabold text-slate-900 tracking-tight">
            Patientenaufnahme
          </h1>
          <p className="text-lg text-slate-500 font-medium max-w-[280px] mx-auto">
            Sichere, verschlüsselte Verbindung zur Apotheke.
          </p>
        </div>
        
        <Button 
          onClick={startSession} 
          className="bg-red-600 hover:bg-red-700 text-white w-full h-16 text-xl font-bold rounded-xl shadow-md transition-all active:scale-95"
        >
          Aufnahme starten
        </Button>
        
        <div className="text-xs text-slate-400 font-medium pt-8 flex flex-col items-center gap-2">
          <span className="flex items-center gap-1">
            <span className="w-2 h-2 rounded-full bg-green-500"></span>
            Verbindung verschlüsselt (TLS 1.3)
          </span>
          <span>KBV-zertifizierter Datenschutz</span>
          <div className="flex gap-4 mt-2">
            <a href="/impressum" target="_blank" className="hover:text-slate-600 underline transition-colors">Impressum</a>
            <a href="/datenschutz" target="_blank" className="hover:text-slate-600 underline transition-colors">Datenschutzerklärung</a>
          </div>
        </div>
      </div>
    </div>
  );
}
