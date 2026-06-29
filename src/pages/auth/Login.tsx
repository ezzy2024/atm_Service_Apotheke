import { useState } from 'react';
import { supabase } from '@/lib/supabase';
import { Button } from '@/components/ui/button';
import { useNavigate } from 'react-router-dom';
import { Stethoscope, ArrowLeft } from 'lucide-react';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);
    
    const { error } = await supabase.auth.signInWithPassword({ email, password });

    if (error) {
      if (error.message.includes("Email not confirmed")) {
        setError('E-Mail noch nicht bestätigt. Bitte prüfen Sie Ihr Postfach oder deaktivieren Sie die E-Mail-Bestätigung in den Supabase-Einstellungen.');
      } else {
        setError('Anmeldung fehlgeschlagen. Bitte prüfen Sie Ihre Zugangsdaten.');
      }
      setIsLoading(false);
    } else {
      // Sicherheitsbereinigung: Alte Demo-Daten strikt löschen
      localStorage.removeItem("demo_auth_token");
      localStorage.removeItem("demo_pharmacy_id");
      localStorage.removeItem("demo_role");
      localStorage.removeItem("demo_pharmacist_name");
      
      // Redirect
      navigate('/admin/dashboard');
    }
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-slate-50 font-sans p-4 relative">
      <a 
        href="https://serviceapotheke.tech" 
        className="absolute top-8 left-8 flex items-center gap-2 text-slate-500 hover:text-[#0082C8] transition-colors text-sm font-semibold"
      >
        <ArrowLeft className="w-4 h-4" /> Zurück zur Homepage (serviceapotheke.tech)
      </a>

      <form onSubmit={handleLogin} className="p-10 bg-white rounded-2xl shadow-xl w-full max-w-md space-y-8 border border-slate-100">
        <div className="space-y-4">
          <div className="w-16 h-16 bg-[#0082C8]/10 rounded-2xl flex items-center justify-center mx-auto mb-2">
            <Stethoscope className="w-8 h-8 text-[#0082C8]" />
          </div>
          <h1 className="text-3xl font-bold text-slate-900 text-center tracking-tight">Service Apotheke aTM</h1>
          <p className="text-slate-500 text-center text-sm font-medium">
            B2B-Portal für assistierte Telemedizin
          </p>
        </div>
        
        {error && (
          <div className="p-4 bg-red-50 border border-red-200 text-red-700 rounded-lg text-sm font-medium leading-relaxed">
            {error}
          </div>
        )}
        
        <div className="space-y-5">
          <div className="space-y-2">
            <label className="text-sm font-semibold text-slate-700">Admin E-Mail</label>
            <input 
              type="email" 
              placeholder="zz.B. admin@stadt-apotheke.de" 
              className="w-full p-3.5 border border-slate-200 rounded-xl focus:ring-2 focus:ring-[#0082C8] outline-none transition-all text-sm bg-slate-50 focus:bg-white" 
              value={email} 
              onChange={(e) => setEmail(e.target.value)} 
              required 
            />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-semibold text-slate-700">Passwort</label>
            <input 
              type="password" 
              placeholder="••••••••" 
              className="w-full p-3.5 border border-slate-200 rounded-xl focus:ring-2 focus:ring-[#0082C8] outline-none transition-all text-sm bg-slate-50 focus:bg-white" 
              value={password} 
              onChange={(e) => setPassword(e.target.value)} 
              required 
            />
          </div>
        </div>

        <Button 
          type="submit" 
          disabled={isLoading} 
          className="w-full bg-[#0082C8] text-white hover:bg-[#006A9C] py-6 rounded-xl text-lg font-bold transition-all disabled:opacity-50 cursor-pointer shadow-md shadow-[#0082C8]/20"
        >
          {isLoading ? 'Wird authentifiziert...' : 'Sicher Anmelden'}
        </Button>

        <div className="text-center text-sm text-slate-500 pt-6 border-t border-slate-100 flex flex-col gap-2">
          <span className="font-medium">Noch kein aTM-Partner?</span>
          <Button
            type="button"
            variant="link"
            onClick={() => navigate("/register")}
            className="text-[#0082C8] p-0 h-auto font-bold text-sm hover:text-[#006A9C]"
          >
            Jetzt als Apotheke registrieren
          </Button>
        </div>
      </form>
    </div>
  );
}
