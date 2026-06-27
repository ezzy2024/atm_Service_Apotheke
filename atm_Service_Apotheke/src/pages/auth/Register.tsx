import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Stethoscope } from 'lucide-react';
import { supabase } from '@/lib/supabase';

export default function Register() {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    name: '',
    ik_nummer: '',
    bsnr: '',
    ansprechpartner: '',
    telefon: '',
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({ ...formData, [e.target.id]: e.target.value });
  };

  const handleRegister = async () => {
    if (!formData.name || !formData.ik_nummer || !formData.bsnr || !formData.ansprechpartner) {
      alert("Bitte füllen Sie alle Pflichtfelder aus.");
      return;
    }

    try {
      const response = await fetch("/api/admin/pharmacies", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          name: formData.name,
          ik_nummer: formData.ik_nummer,
          bsnr: formData.bsnr,
          ansprechpartner: formData.ansprechpartner,
          telefon: formData.telefon,
          status: 'pending',
          is_approved: false
        }),
      });

      if (!response.ok) {
        const errData = await response.json();
        throw new Error(errData.error || "Failed to register");
      }

      alert("Registrierung erfolgreich. Der Super-Admin wird Ihre Anfrage prüfen.");
      navigate('/login');
    } catch (e: any) {
      console.error(e);
      alert(`Fehler bei der Registrierung: ${e.message}`);
    }
  };

  return (
    <div className="min-h-screen bg-slate-50 flex items-center justify-center p-4">
      <Card className="w-full max-w-md shadow-lg border-slate-200">
        <CardHeader className="text-center space-y-2">
          <div className="w-16 h-16 bg-[#0082C8]/10 rounded-full flex items-center justify-center mx-auto mb-4">
            <Stethoscope className="w-8 h-8 text-[#0082C8]" />
          </div>
          <CardTitle className="text-2xl font-bold text-slate-900 tracking-tight">Apotheke Registrieren</CardTitle>
          <CardDescription className="text-slate-500">Erstellen Sie einen B2B Account für das aTM-Cockpit.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="name">Apotheken-Name *</Label>
            <Input id="name" value={formData.name} onChange={handleChange} placeholder="Stadt-Apotheke" />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="ik_nummer">IK-Nummer *</Label>
              <Input id="ik_nummer" value={formData.ik_nummer} onChange={handleChange} placeholder="123456789" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="bsnr">BSNR *</Label>
              <Input id="bsnr" value={formData.bsnr} onChange={handleChange} placeholder="000000000" />
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="ansprechpartner">Ansprechpartner / Admin-Email *</Label>
            <Input id="ansprechpartner" value={formData.ansprechpartner} onChange={handleChange} placeholder="Max Mustermann / email@example.com" />
          </div>
          <div className="space-y-2">
            <Label htmlFor="telefon">Telefon (Optional)</Label>
            <Input id="telefon" value={formData.telefon} onChange={handleChange} placeholder="+49 123 456789" />
          </div>
        </CardContent>
        <CardFooter className="flex-col gap-4">
          <Button className="w-full bg-[#0082C8] hover:bg-[#006A9C] text-white py-6 text-lg" onClick={handleRegister}>
            Registrierung absenden
          </Button>
          <Button variant="link" onClick={() => navigate('/login')} className="text-slate-500">
            Zurück zum Login
          </Button>
        </CardFooter>
      </Card>
    </div>
  );
}
