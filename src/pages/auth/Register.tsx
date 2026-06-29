import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Stethoscope, ArrowLeft, ArrowRight } from 'lucide-react';

export default function Register() {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    name: '',
    ik_nummer: '',
    bsnr: '',
    admin_email: '',
    password: '',
    telefon: '',
  });
  const [isLoading, setIsLoading] = useState(false);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({ ...formData, [e.target.id]: e.target.value });
  };

  const handleRegister = async () => {
    if (!formData.name || !formData.ik_nummer || !formData.bsnr || !formData.admin_email || !formData.password) {
      alert("Bitte füllen Sie alle Pflichtfelder aus.");
      return;
    }

    setIsLoading(true);
    try {
      const res = await fetch("/api/auth/register-b2b", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(formData),
      });

      if (!res.ok) {
        const errorData = await res.json();
        throw new Error(errorData.error || "Netzwerkfehler");
      }

      alert("Registrierung erfolgreich! Bitte loggen Sie sich ein, um den Onboarding-Prozess zu starten.");
      navigate('/login');
    } catch (e: any) {
      console.error(e);
      alert(`Fehler bei der Registrierung: ${e.message}`);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-50 flex items-center justify-center p-4 relative font-sans">
      <a 
        href="https://serviceapotheke.tech" 
        className="absolute top-8 left-8 flex items-center gap-2 text-slate-500 hover:text-[#0082C8] transition-colors text-sm font-semibold"
      >
        <ArrowLeft className="w-4 h-4" /> Zurück zur Homepage
      </a>

      <Card className="w-full max-w-xl shadow-xl border-slate-100 rounded-2xl p-4">
        <CardHeader className="text-center space-y-4">
          <div className="w-16 h-16 bg-[#0082C8]/10 rounded-2xl flex items-center justify-center mx-auto mb-2">
            <Stethoscope className="w-8 h-8 text-[#0082C8]" />
          </div>
          <CardTitle className="text-3xl font-bold text-slate-900 tracking-tight">Partner-Apotheke werden</CardTitle>
          <CardDescription className="text-slate-500 font-medium">Erstellen Sie einen B2B Account für das aTM-Cockpit.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6 pt-4">
          <div className="space-y-2">
            <Label htmlFor="name" className="text-slate-700 font-semibold">Offizieller Apotheken-Name *</Label>
            <Input id="name" value={formData.name} onChange={handleChange} placeholder="z.B. Stadt-Apotheke am Markt" className="p-5 bg-slate-50 border-slate-200 focus:bg-white rounded-xl" />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="ik_nummer" className="text-slate-700 font-semibold">IK-Nummer *</Label>
              <Input id="ik_nummer" value={formData.ik_nummer} onChange={handleChange} placeholder="9-stellig" className="p-5 bg-slate-50 border-slate-200 focus:bg-white rounded-xl" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="bsnr" className="text-slate-700 font-semibold">BSNR *</Label>
              <Input id="bsnr" value={formData.bsnr} onChange={handleChange} placeholder="Betriebsstättennummer" className="p-5 bg-slate-50 border-slate-200 focus:bg-white rounded-xl" />
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="admin_email" className="text-slate-700 font-semibold">Administrator E-Mail *</Label>
            <Input id="admin_email" type="email" value={formData.admin_email} onChange={handleChange} placeholder="admin@apotheke.de" className="p-5 bg-slate-50 border-slate-200 focus:bg-white rounded-xl" />
          </div>
          <div className="space-y-2">
            <Label htmlFor="password" className="text-slate-700 font-semibold">Sicheres Passwort *</Label>
            <Input id="password" type="password" value={formData.password} onChange={handleChange} placeholder="Mindestens 8 Zeichen" className="p-5 bg-slate-50 border-slate-200 focus:bg-white rounded-xl" />
          </div>
        </CardContent>
        <CardFooter className="flex-col gap-6 pt-4 border-t border-slate-100 mt-4">
          <Button 
            disabled={isLoading}
            className="w-full bg-[#0082C8] hover:bg-[#006A9C] text-white py-6 rounded-xl text-lg font-bold transition-all shadow-md shadow-[#0082C8]/20 flex items-center justify-center gap-2" 
            onClick={handleRegister}
          >
            {isLoading ? "Wird verarbeitet..." : "Konto erstellen"} <ArrowRight className="w-5 h-5" />
          </Button>
          <div className="text-sm font-medium text-slate-500">
            Bereits registriert?{" "}
            <Button variant="link" onClick={() => navigate('/login')} className="text-[#0082C8] hover:text-[#006A9C] font-bold p-0">
              Hier anmelden
            </Button>
          </div>
        </CardFooter>
      </Card>
    </div>
  );
}
