import { Outlet, Navigate, useLocation } from 'react-router-dom';
import { Volume2, VolumeX } from 'lucide-react';
import { useAudioAssistant } from '@/src/hooks/useAudioAssistant';

export default function KioskLayout() {
  const token = localStorage.getItem("kiosk_device_token");
  const location = useLocation();
  const { isEnabled, toggleAudio } = useAudioAssistant();

  if (!token && location.pathname !== "/kiosk/pairing") {
    return <Navigate to="/kiosk/pairing" replace />;
  }

  return (
    <div className="h-screen w-screen bg-slate-50 flex flex-col text-slate-900 overflow-hidden">
      <header className="h-20 bg-[#003366] flex items-center justify-between px-8 shadow-md shrink-0">
        <h1 className="text-2xl font-bold text-white tracking-tight">Service Apotheke aTM</h1>
        
        {location.pathname !== "/kiosk/pairing" && (
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
        )}
      </header>
      
      <main className="flex-1 overflow-auto p-8 flex items-center justify-center">
        <div className="max-w-4xl w-full h-full bg-white rounded-2xl shadow-lg border border-slate-200 flex flex-col overflow-hidden relative">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
