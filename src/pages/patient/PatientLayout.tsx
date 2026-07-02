import { Outlet, useSearchParams } from 'react-router-dom';
import { Volume2, VolumeX, ShieldAlert } from 'lucide-react';
import { useAudioAssistant } from '@/src/hooks/useAudioAssistant';
import { useEffect, useState } from 'react';

export default function PatientLayout() {
  const [searchParams] = useSearchParams();
  const { isEnabled, toggleAudio } = useAudioAssistant();
  const [hasValidPharmacy, setHasValidPharmacy] = useState(true);

  useEffect(() => {
    const urlPharmacyId = searchParams.get('pharmacy_id');
    if (urlPharmacyId) {
      sessionStorage.setItem('byod_pharmacy_id', urlPharmacyId);
      setHasValidPharmacy(true);
    } else {
      const storedPharmacyId = sessionStorage.getItem('byod_pharmacy_id');
      if (!storedPharmacyId) {
        setHasValidPharmacy(false);
      }
    }
  }, [searchParams]);

  if (!hasValidPharmacy) {
    return (
      <div className="h-screen w-screen bg-slate-50 flex flex-col items-center justify-center p-6 text-center">
        <ShieldAlert className="w-16 h-16 text-red-600 mb-4" />
        <h2 className="text-2xl font-bold text-slate-900 mb-2">Ungültiger Zugangscode</h2>
        <p className="text-slate-500 max-w-md">
          Bitte scannen Sie den QR-Code Ihrer Apotheke erneut, um die digitale Patientenaufnahme zu starten.
        </p>
      </div>
    );
  }

  return (
    <div className="h-screen w-screen bg-slate-50 flex flex-col text-slate-900 overflow-hidden">
      <header className="h-20 bg-red-600 flex items-center justify-between px-8 shadow-md shrink-0">
        <h1 className="text-2xl font-bold text-white tracking-tight">Digitale Apotheken-Rezeption</h1>
        
        <button 
            onClick={toggleAudio}
            className="flex items-center gap-3 bg-white/10 hover:bg-white/20 text-white px-6 py-3 rounded-xl transition-colors h-14"
            aria-label={isEnabled ? "Audio deaktivieren" : "Audio aktivieren"}
          >
            {isEnabled ? (
              <>
                <Volume2 className="w-8 h-8" />
                <span className="font-bold text-lg hidden sm:inline">Vorlesen: An</span>
              </>
            ) : (
              <>
                <VolumeX className="w-8 h-8 opacity-50" />
                <span className="font-bold text-lg opacity-50 hidden sm:inline">Vorlesen: Aus</span>
              </>
            )}
          </button>
      </header>
      
      <main className="flex-1 overflow-auto p-8 flex items-center justify-center">
        <div className="max-w-4xl w-full h-full bg-white rounded-2xl shadow-lg border border-slate-200 flex flex-col overflow-hidden relative">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
