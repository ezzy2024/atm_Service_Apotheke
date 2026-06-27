import { useState } from "react";
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
import { ShieldCheck, Mail } from "lucide-react";
import { supabase } from "@/lib/supabase";

export default function Settings() {
  const [ikNumber, setIkNumber] = useState("123456789");
  const [bsnr, setBsnr] = useState("000000000");
  const [avvAccepted, setAvvAccepted] = useState(false);
  const [avvSavedDate, setAvvSavedDate] = useState<string | null>(null);
  const [inviteEmail, setInviteEmail] = useState("");
  const [isSaved, setIsSaved] = useState(false);

  const handleSaveAVV = async () => {
    if (!avvAccepted) {
      alert("Bitte akzeptieren Sie den AVV.");
      return;
    }

    try {
      // In a real app we would use auth to get the pharmacy_id
      // await supabase.from('pharmacies').update({ avv_akzeptiert_am: new Date().toISOString() }).eq('id', pharmacyId);
      setAvvSavedDate(new Date().toISOString());
      setIsSaved(true);
      setTimeout(() => setIsSaved(false), 3000);
    } catch (e) {
      console.error(e);
      alert("Fehler beim Speichern");
    }
  };

  const handleInvite = () => {
    if (!avvSavedDate) {
      alert("Bitte akzeptieren Sie zuerst den AVV.");
      return;
    }
    if (!inviteEmail) return;
    alert(`Einladung an ${inviteEmail} wurde versendet.`);
    setInviteEmail("");
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
          <CardFooter>
            <Button
              onClick={handleSaveAVV}
              className="bg-[#0082C8] hover:bg-[#006A9C] text-white"
            >
              {isSaved ? "Gespeichert!" : "Daten & AVV speichern"}
            </Button>
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
      </div>
    </div>
  );
}
