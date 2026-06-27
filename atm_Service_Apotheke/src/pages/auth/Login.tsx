import { useState } from 'react';
import { supabase } from '@/lib/supabase';
import { Button } from '@/components/ui/button';
import { useNavigate } from 'react-router-dom';

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
      setError('Anmeldung fehlgeschlagen. Bitte prüfen Sie Ihre Zugangsdaten.');
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
    <div className="flex items-center justify-center min-h-screen bg-slate-50">
      <form onSubmit={handleLogin} className="p-8 bg-white rounded-xl shadow-lg w-full max-w-md space-y-6 border border-slate-200">
        <h1 className="text-2xl font-bold text-slate-800 text-center">Apotheken-Cockpit</h1>
        <p className="text-slate-500 text-center text-sm font-normal">
          Loggen Sie sich ein, um auf Ihre aTM-Daten zuzugreifen.
        </p>
        
        {error && (
          <div className="p-4 bg-red-50 border border-red-200 text-red-700 rounded-lg text-sm font-medium">
            {error}
          </div>
        )}
        
        <div className="space-y-4">
          <input 
            type="email" 
            placeholder="Ihre E-Mail-Adresse" 
            className="w-full p-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#0082C8] outline-none transition-all text-sm" 
            value={email} 
            onChange={(e) => setEmail(e.target.value)} 
            required 
          />
          <input 
            type="password" 
            placeholder="Passwort" 
            className="w-full p-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#0082C8] outline-none transition-all text-sm" 
            value={password} 
            onChange={(e) => setPassword(e.target.value)} 
            required 
          />
        </div>

        <Button 
          type="submit" 
          disabled={isLoading} 
          className="w-full bg-[#0082C8] text-white hover:bg-[#006A9C] py-6 rounded-lg text-lg font-bold transition-all disabled:opacity-50 cursor-pointer"
        >
          {isLoading ? 'Wird authentifiziert...' : 'Sicher Anmelden'}
        </Button>

        <div className="text-center text-sm text-slate-500 pt-2 border-t border-slate-100 flex flex-col gap-2">
          <span>Noch kein Cockpit-Konto?</span>
          <Button
            type="button"
            variant="link"
            onClick={() => navigate("/register")}
            className="text-[#0082C8] p-0 h-auto font-bold text-sm"
          >
            Apotheke registrieren (B2B)
          </Button>
        </div>
      </form>
    </div>
  );
}
