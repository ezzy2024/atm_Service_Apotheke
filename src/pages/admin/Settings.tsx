import { useState, useEffect } from "react";
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  CardFooter,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { ShieldCheck, Mail, FileText } from "lucide-react";
import { supabase } from "@/lib/supabase";

export default function Settings() {
  const [ikNumber, setIkNumber] = useState("123456789");
  const [bsnr, setBsnr] = useState("000000000");
  const [avvAccepted, setAvvAccepted] = useState(false);
  const [avvSavedDate, setAvvSavedDate] = useState<string | null>(null);
  const [inviteEmail, setInviteEmail] = useState("");
  const [isSaved, setIsSaved] = useState(false);

  // Fetch initial AVV status
  useEffect(() => {
    const fetchPharmacy = async () => {
      try {
        const { data: { user } } = await supabase.auth.getUser();
        if (!user) return;
        
        const { data: profile } = await supabase.from("profiles").select("pharmacy_id").eq("id", user.id).single();
        if (!profile) return;
        
        const { data: pharm } = await supabase.from("pharmacies").select("avv_akzeptiert_am").eq("id", profile.pharmacy_id).single();
        if (pharm && pharm.avv_akzeptiert_am) {
          setAvvAccepted(true);
          setAvvSavedDate(pharm.avv_akzeptiert_am);
        }
      } catch (e) {
        console.error("Failed to load pharmacy settings:", e);
      }
    };
    fetchPharmacy();
  }, []);

  const handleSaveAVV = async () => {
    if (!avvAccepted) {
      alert("Bitte akzeptieren Sie den AVV.");
      return;
    }

    try {
      const pharmacyId = localStorage.getItem("demo_pharmacy_id") || "d3b07384-d113-4956-a50e-a1c563e4410a";
      const response = await fetch("/api/admin/accept-avv", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ pharmacy_id: pharmacyId })
      });
      
      const data = await response.json();
      if (!response.ok) throw new Error(data.error);

      setAvvSavedDate(data.avv_akzeptiert_am);
      setIsSaved(true);
      setTimeout(() => setIsSaved(false), 3000);
    } catch (e: any) {
      console.error(e);
      alert("Fehler beim Speichern: " + e.message);
    }
  };

  const handleInvite = async () => {
    if (!avvSavedDate) {
      alert("Bitte akzeptieren Sie zuerst den AVV.");
      return;
    }
    if (!inviteEmail) return;
    
    try {
      // In a real app we would get this from context or auth session
      const pharmacyId = localStorage.getItem("demo_pharmacy_id") || "d3b07384-d113-4956-a50e-a1c563e4410a";
      const response = await fetch("/api/admin/invite", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: inviteEmail, pharmacy_id: pharmacyId })
      });
      const data = await response.json();
      
      if (!response.ok) throw new Error(data.error);
      
      alert(`Einladung an ${inviteEmail} wurde erfolgreich versendet!`);
      setInviteEmail("");
    } catch(e: any) {
      alert(`Fehler beim Einladen: ${e.message}`);
    }
  };

  return (
    <div className="max-w-4xl mx-auto space-y-8 animate-in fade-in duration-500">
      <div>
        <h1 className="text-3xl font-bold text-slate-900 tracking-tight">
          Einstellungen & Compliance
        </h1>
        <p className="text-slate-500 mt-2 text-lg">
          Verwalten Sie Ihre Apotheken-Daten und
          DSGVO-Auftragsverarbeitungsverträge (AVV).
        </p>
      </div>

      <div className="grid grid-cols-1 gap-8">
        <Card className="border-slate-200 shadow-sm">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <ShieldCheck className="w-5 h-5 text-[#0082C8]" />
              DSGVO AVV & Stammdaten
            </CardTitle>
            <CardDescription>
              Gemäß DSGVO müssen Sie als Apotheken-Administrator den
              Auftragsverarbeitungsvertrag abschließen.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="ik">Institutionskennzeichen (IK)</Label>
                <Input
                  id="ik"
                  value={ikNumber}
                  onChange={(e) => setIkNumber(e.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="bsnr">Betriebsstättennummer (BSNR)</Label>
                <Input
                  id="bsnr"
                  value={bsnr}
                  onChange={(e) => setBsnr(e.target.value)}
                />
              </div>
            </div>

            <div className="p-4 bg-slate-50 border border-slate-200 rounded-lg space-y-4">
              <div className="flex items-start gap-3">
                <Checkbox
                  id="avv"
                  checked={avvAccepted}
                  onCheckedChange={(checked) =>
                    setAvvAccepted(checked as boolean)
                  }
                  className="mt-1"
                />
                <div className="space-y-1">
                  <Label
                    htmlFor="avv"
                    className="text-base font-medium leading-none"
                  >
                    Auftragsverarbeitungsvertrag (AVV) akzeptieren
                  </Label>
                  <p className="text-sm text-slate-500">
                    Hiermit bestätige ich, dass ich den digitalen AVV nach Art.
                    28 DSGVO gelesen habe und im Namen der Apotheke rechtlich
                    bindend akzeptiere.
                  </p>
                </div>
              </div>
            </div>
          </CardContent>
          <CardFooter className="flex justify-between items-center">
            <Button
              onClick={handleSaveAVV}
              className="bg-[#0082C8] hover:bg-[#006A9C] text-white"
            >
              {isSaved ? "Gespeichert!" : "Daten & AVV speichern"}
            </Button>
            
            {avvSavedDate && (
              <Button
                variant="outline"
                className="gap-2 border-[#0082C8] text-[#0082C8] hover:bg-[#0082C8]/10"
                onClick={() => {
                  const pharmacyId = localStorage.getItem("demo_pharmacy_id");
                  if (pharmacyId) {
                    window.open(`/api/admin/audit-log/${pharmacyId}`, "_blank");
                  }
                }}
              >
                <FileText className="w-4 h-4" />
                DSGVO-Verfahrensverzeichnis (PDF) laden
              </Button>
            )}
          </CardFooter>
        </Card>

        <Card
          className={`border-slate-200 shadow-sm ${!avvSavedDate ? "opacity-50 pointer-events-none" : ""}`}
        >
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Mail className="w-5 h-5 text-[#0082C8]" />
              Apotheker Einladen
            </CardTitle>
            <CardDescription>
              {avvSavedDate
                ? "Laden Sie Ihre Mitarbeiter ein, das Apotheken-Cockpit zu nutzen."
                : "Bitte akzeptieren Sie zuerst den AVV, um Mitarbeiter einzuladen."}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-end gap-4">
              <div className="space-y-2 flex-1">
                <Label htmlFor="email">E-Mail Adresse</Label>
                <Input
                  id="email"
                  type="email"
                  placeholder="apotheker@example.com"
                  value={inviteEmail}
                  onChange={(e) => setInviteEmail(e.target.value)}
                />
              </div>
              <Button onClick={handleInvite} variant="outline">
                Einladung senden
              </Button>
            </div>
          </CardContent>
        </Card>

        {/* Subscription / Billing Card */}
        <Card className="border-slate-200 shadow-sm bg-gradient-to-br from-white to-[#0082C8]/5 border-l-4 border-l-[#0082C8]">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-slate-900">
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-[#0082C8]"><rect width="20" height="14" x="2" y="5" rx="2"/><line x1="2" x2="22" y1="10" y2="10"/></svg>
              Abonnement & Abrechnung
            </CardTitle>
            <CardDescription className="text-slate-600">
              Verwalten Sie Ihre Telepharmazie-Lizenz.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="p-4 bg-white border border-slate-200 rounded-lg flex justify-between items-center shadow-sm">
              <div>
                <h4 className="font-bold text-slate-900">aTM Professional Lizenz</h4>
                <p className="text-sm text-slate-500">Unbegrenzte Telepharmazie-Sitzungen & pDL-Abrechnung</p>
              </div>
              <div className="text-right">
                <span className="font-bold text-xl text-[#0082C8]">199 €</span><span className="text-sm text-slate-500"> / Monat</span>
              </div>
            </div>
          </CardContent>
          <CardFooter>
            <Button
              className="bg-slate-100 hover:bg-slate-200 text-slate-700 w-full font-semibold shadow-sm border border-slate-300"
              onClick={() => {
                alert("Stripe-Integration folgt im nächsten Schritt. Sie können das System vorerst ohne Zahlungsdaten nutzen.");
              }}
            >
              Später bezahlen (Demo-Modus aktiv)
            </Button>
          </CardFooter>
        </Card>
      </div>
    </div>
  );
}
