import { useNavigate } from 'react-router-dom';
import { Monitor } from 'lucide-react';
import { Button } from '@/components/ui/button';

export default function Standby() {
  const navigate = useNavigate();

  const startSession = () => {
    // Generate a new session ID
    const sessionId = crypto.randomUUID();
    navigate(`/kiosk/session/${sessionId}`);
  };

  return (
    <div className="flex-1 flex flex-col items-center justify-center p-12 text-center bg-slate-50">
      <div className="w-24 h-24 bg-[#0082C8]/10 rounded-full flex items-center justify-center mb-8">
        <Monitor className="w-12 h-12 text-[#0082C8]" />
      </div>
      <h2 className="text-4xl font-bold text-slate-900 mb-4 tracking-tight">Assistierte Telemedizin (aTM)</h2>
      <p className="text-xl text-slate-600 mb-12 max-w-lg">
        Willkommen in unserer Apotheke. Bitte berühren Sie den Bildschirm, um Ihre Sitzung zu starten.
      </p>
      
      <Button 
        onClick={startSession} 
        className="bg-[#0082C8] hover:bg-[#006A9C] text-white px-12 py-8 text-2xl rounded-xl shadow-lg transition-transform active:scale-95"
      >
        Sitzung starten
      </Button>
    </div>
  );
}
