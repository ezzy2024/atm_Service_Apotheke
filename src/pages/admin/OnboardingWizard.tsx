import { useState, useEffect } from "react";
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Upload, FileText, CheckCircle2, Clock, AlertTriangle, LogOut } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { supabase } from "@/lib/supabase";

import { trackEvent } from "@/src/lib/analytics";

export default function OnboardingWizard() {
  const navigate = useNavigate();
  const [pharmacy, setPharmacy] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState<string | null>(null);

  useEffect(() => {
    fetchPharmacyData();
    trackEvent('funnel_step', { step: 'registration_started' });
  }, []);

  const fetchPharmacyData = async () => {
    try {
      const { data: { user } } = await supabase.auth.getUser();
      if (!user) {
        navigate("/login");
        return;
      }

      // Fetch profile
      const { data: profile } = await supabase
        .from("profiles")
        .select("pharmacy_id")
        .eq("id", user.id)
        .single();

      if (!profile) return;

      // Fetch pharmacy details
      const { data: pharm } = await supabase
        .from("pharmacies")
        .select("*")
        .eq("id", profile.pharmacy_id)
        .single();

      setPharmacy(pharm);
      
      // Update local storage so AdminLayout can sync
      if (pharm) {
        localStorage.setItem("demo_pharmacy_id", pharm.id);
        localStorage.setItem("demo_role", "pharmacy_admin");
        localStorage.setItem("demo_pharmacist_name", pharm.name);

        // Track state-based funnel events
        if (pharm.onboarding_status === 'pending_approval') {
          trackEvent('funnel_step', { step: 'data_entry_completed' });
        } else if (pharm.onboarding_status === 'pending_verification') {
          trackEvent('funnel_complete', { action: 'onboarding_success' });
        }
      }
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>, docType: string) => {
    const file = e.target.files?.[0];
    if (!file || !pharmacy) return;

    setUploading(docType);

    try {
      // Read file as base64
      const reader = new FileReader();
      reader.onload = async () => {
        const base64 = (reader.result as string).split(',')[1];
        
        const response = await fetch("/api/pharmacy/upload-document", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            pharmacy_id: pharmacy.id,
            doc_type: docType,
            file_base64: base64
          }),
        });

        if (!response.ok) {
          throw new Error("Dateiupload fehlgeschlagen");
        }

        trackEvent('funnel_step', { step: 'document_upload', doc_type: docType });
        alert("Dokument erfolgreich hochgeladen!");
        fetchPharmacyData();
      };
      reader.readAsDataURL(file);
    } catch (error: any) {
      alert(`Fehler: ${error.message}`);
    } finally {
      setUploading(null);
    }
  };

  const handleLogout = async () => {
    await supabase.auth.signOut();
    localStorage.clear();
    navigate("/login");
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[500px]">
        <div className="text-slate-500 font-medium">Lade Onboarding-Status...</div>
      </div>
    );
  }

  if (!pharmacy) {
    return (
      <div className="p-8 text-center bg-white rounded-lg shadow-sm border border-slate-200">
        <AlertTriangle className="w-12 h-12 text-red-500 mx-auto mb-4" />
        <h3 className="text-lg font-bold text-slate-900">Fehler beim Laden</h3>
        <p className="text-slate-500 mt-2">Ihre Apotheke konnte nicht zugeordnet werden.</p>
        <Button onClick={handleLogout} className="mt-4 bg-[#0082C8] text-white">Abmelden</Button>
      </div>
    );
  }

  // Steps definitions
  const steps = [
    {
      num: 1,
      title: "Registrierung eingegangen",
      desc: "Ihr B2B-Konto wurde erfolgreich erstellt.",
      status: "completed",
    },
    {
      num: 2,
      title: "Erstfreigabe durch Administrator",
      desc: "Administrative Prüfung Ihrer Stammdaten (IK-Nummer, BSNR).",
      status: pharmacy.onboarding_status === "pending_approval" ? "current" : "completed",
    },
    {
      num: 3,
      title: "Vertragsunterlagen & Dokumente hochladen",
      desc: "Bitte laden Sie die erforderlichen B2B-Nachweise hoch.",
      status: 
        pharmacy.onboarding_status === "pending_approval" 
          ? "locked" 
          : pharmacy.onboarding_status === "pending_documents" 
            ? "current" 
            : "completed",
    },
    {
      num: 4,
      title: "Abonnement",
      desc: "Schließen Sie das B2B-Abonnement ab.",
      status: 
        pharmacy.onboarding_status === "pending_verification" || pharmacy.onboarding_status === "active" 
          ? pharmacy.subscription_status === "active" ? "completed" : "current"
          : "locked",
    },
    {
      num: 5,
      title: "Aktivierung",
      desc: "Prüfung Ihrer Dokumente und Freischaltung des Kiosk-Betriebs.",
      status: 
        pharmacy.onboarding_status === "active" && pharmacy.subscription_status === "active"
          ? "completed" 
          : "locked",
    },
  ];

  return (
    <div className="max-w-4xl mx-auto space-y-8 animate-in fade-in duration-500">
      <div>
        <h1 className="text-3xl font-bold text-slate-900 tracking-tight">Onboarding & Freischaltung</h1>
        <p className="text-slate-500 mt-2 text-lg">
          Willkommen bei der **Service Apotheke aTM**. Bitte schließen Sie die folgenden Schritte ab.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        {steps.map((step) => (
          <Card key={step.num} className={`border-slate-200 shadow-xs relative overflow-hidden ${
            step.status === "current" ? "border-l-4 border-l-[#0082C8] bg-[#0082C8]/5" : ""
          } ${step.status === "locked" ? "opacity-60" : ""}`}>
            <CardHeader className="p-4 flex flex-row items-start justify-between space-y-0">
              <div className="flex items-center gap-3">
                <div className={`w-8 h-8 rounded-full flex items-center justify-center font-bold text-sm ${
                  step.status === "completed" 
                    ? "bg-green-100 text-green-800" 
                    : step.status === "current" 
                      ? "bg-[#0082C8] text-white" 
                      : "bg-slate-100 text-slate-500"
                }`}>
                  {step.status === "completed" ? <CheckCircle2 className="w-5 h-5" /> : step.num}
                </div>
                <div className="font-semibold text-sm text-slate-900">{step.title}</div>
              </div>
            </CardHeader>
            <CardContent className="p-4 pt-0 text-xs text-slate-500">
              {step.desc}
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Main interaction section */}
      <Card className="border-slate-200 shadow-sm">
        <CardHeader>
          <CardTitle>Status: {
            pharmacy.onboarding_status === "pending_approval" && "Warten auf Erstfreigabe"
          }{
            pharmacy.onboarding_status === "pending_documents" && "Dokumente ausstehend"
          }{
            pharmacy.onboarding_status === "pending_verification" && "Dokumente in Prüfung"
          }</CardTitle>
          <CardDescription>
            {pharmacy.onboarding_status === "pending_approval" && "Ein Administrator prüft derzeit Ihre Registrierungsdaten (IK-Nummer und BSNR). Sobald diese freigegeben sind, können Sie Ihre Nachweise hochladen."}
            {pharmacy.onboarding_status === "pending_documents" && "Bitte laden Sie die folgenden drei Unterlagen im PDF-Format hoch, um Ihr Konto zu verifizieren."}
            {pharmacy.onboarding_status === "pending_verification" && "Vielen Dank! Ihre Unterlagen wurden vollständig hochgeladen und werden jetzt von unserem Team verifiziert. Nach erfolgreicher Prüfung erhalten Sie Vollzugriff."}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {pharmacy.onboarding_status === "pending_approval" && (
            <div className="flex flex-col items-center justify-center p-8 bg-slate-50 rounded-lg border border-dashed border-slate-200">
              <Clock className="w-12 h-12 text-amber-500 animate-pulse mb-3" />
              <div className="font-semibold text-slate-700">Administrative Prüfung läuft</div>
              <div className="text-slate-500 text-sm mt-1 text-center max-w-md">
                Dies dauert in der Regel weniger als 24 Stunden. Sie werden per E-Mail benachrichtigt, sobald dieser Schritt abgeschlossen ist.
              </div>
            </div>
          )}

          {pharmacy.onboarding_status === "pending_verification" && (
            <div className="flex flex-col items-center justify-center p-8 bg-green-50/50 rounded-lg border border-dashed border-green-200">
              <CheckCircle2 className="w-12 h-12 text-green-600 mb-3" />
              <div className="font-semibold text-green-900">Unterlagen erfolgreich eingereicht!</div>
              <div className="text-green-700 text-sm mt-1 text-center max-w-md">
                Unser Team prüft die hochgeladenen Dokumente. Sie erhalten Vollzugriff, sobald die Verifizierung abgeschlossen ist.
              </div>
            </div>
          )}

          {(pharmacy.onboarding_status === "pending_documents" || pharmacy.onboarding_status === "pending_verification") && (
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {/* Document 1: Betriebserlaubnis */}
              <div className="p-5 border border-slate-200 rounded-lg space-y-4 bg-white flex flex-col justify-between">
                <div>
                  <div className="font-semibold text-slate-900 flex items-center gap-2">
                    <FileText className="w-4 h-4 text-slate-500" />
                    1. Betriebserlaubnis
                  </div>
                  <p className="text-xs text-slate-500 mt-2">
                    Gültige Betriebserlaubnis der Apotheke (PDF).
                  </p>
                </div>
                <div className="pt-4">
                  {pharmacy.operating_license_path ? (
                    <div className="text-green-600 text-xs font-semibold flex items-center gap-1">
                      <CheckCircle2 className="w-4 h-4" /> Hochgeladen
                    </div>
                  ) : (
                    <div className="relative">
                      <input
                        type="file"
                        accept="application/pdf"
                        onChange={(e) => handleFileUpload(e, "operating_license")}
                        disabled={uploading !== null}
                        className="hidden"
                        id="operating_license_upload"
                      />
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        className="w-full gap-2 cursor-pointer"
                        disabled={uploading !== null}
                        onClick={() => document.getElementById("operating_license_upload")?.click()}
                      >
                        <Upload className="w-3.5 h-3.5" />
                        {uploading === "operating_license" ? "Lädt..." : "PDF Hochladen"}
                      </Button>
                    </div>
                  )}
                </div>
              </div>

              {/* Document 2: Approbationsurkunde */}
              <div className="p-5 border border-slate-200 rounded-lg space-y-4 bg-white flex flex-col justify-between">
                <div>
                  <div className="font-semibold text-slate-900 flex items-center gap-2">
                    <FileText className="w-4 h-4 text-slate-500" />
                    2. Approbationsurkunde
                  </div>
                  <p className="text-xs text-slate-500 mt-2">
                    Nachweis der Approbationsurkunde des leitenden Apothekers.
                  </p>
                </div>
                <div className="pt-4">
                  {pharmacy.approbationsurkunde_path ? (
                    <div className="text-green-600 text-xs font-semibold flex items-center gap-1">
                      <CheckCircle2 className="w-4 h-4" /> Hochgeladen
                    </div>
                  ) : (
                    <div className="relative">
                      <input
                        type="file"
                        accept="application/pdf"
                        onChange={(e) => handleFileUpload(e, "approbationsurkunde")}
                        disabled={uploading !== null}
                        className="hidden"
                        id="approbationsurkunde_upload"
                      />
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        className="w-full gap-2 cursor-pointer"
                        disabled={uploading !== null}
                        onClick={() => document.getElementById("approbationsurkunde_upload")?.click()}
                      >
                        <Upload className="w-3.5 h-3.5" />
                        {uploading === "approbationsurkunde" ? "Lädt..." : "PDF Hochladen"}
                      </Button>
                    </div>
                  )}
                </div>
              </div>

              {/* Document 3: AVV */}
              <div className="p-5 border border-slate-200 rounded-lg space-y-4 bg-white flex flex-col justify-between">
                <div>
                  <div className="font-semibold text-slate-900 flex items-center gap-2">
                    <FileText className="w-4 h-4 text-slate-500" />
                    3. AVV-Vertrag
                  </div>
                  <p className="text-xs text-slate-500 mt-2">
                    Auftragsverarbeitungsvertrag (AVV). Bitte herunterladen, unterschreiben und hochladen.
                  </p>
                </div>
                <div className="pt-4 space-y-2">
                  <a 
                    href="/avv-vorlage.pdf" 
                    download="AVV-Vertrag-Service-Apotheke.pdf" 
                    target="_blank" 
                    rel="noopener noreferrer"
                  >
                    <Button variant="link" size="sm" className="p-0 text-[#0082C8] text-xs font-semibold h-auto">
                      AVV-Vorlage herunterladen
                    </Button>
                  </a>
                  
                  {pharmacy.avv_document_path ? (
                    <div className="text-green-600 text-xs font-semibold flex items-center gap-1">
                      <CheckCircle2 className="w-4 h-4" /> Hochgeladen
                    </div>
                  ) : (
                    <div className="relative">
                      <input
                        type="file"
                        accept="application/pdf"
                        onChange={(e) => handleFileUpload(e, "avv_document")}
                        disabled={uploading !== null}
                        className="hidden"
                        id="avv_document_upload"
                      />
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        className="w-full gap-2 cursor-pointer"
                        disabled={uploading !== null}
                        onClick={() => document.getElementById("avv_document_upload")?.click()}
                      >
                        <Upload className="w-3.5 h-3.5" />
                        {uploading === "avv_document" ? "Lädt..." : "PDF Hochladen"}
                      </Button>
                    </div>
                  )}
                </div>
              </div>
            </div>
          )}

          {/* Stripe Subscription Step */}
          {(pharmacy.onboarding_status === "pending_verification" || pharmacy.onboarding_status === "active") && pharmacy.subscription_status !== "active" && (
            <div className="flex flex-col items-center justify-center p-8 bg-blue-50/50 rounded-lg border border-dashed border-blue-200 mt-6">
              <h3 className="font-semibold text-slate-900 text-lg mb-2">B2B-Abonnement abschließen</h3>
              <p className="text-slate-500 text-sm text-center max-w-md mb-6">
                Um die aTM-Plattform vollumfänglich nutzen zu können, benötigen Sie ein aktives Abonnement. 
                Die Abrechnung erfolgt sicher über Stripe.
              </p>
              <Button 
                onClick={async () => {
                  try {
                    const res = await fetch("/api/stripe/create-checkout-session", {
                      method: "POST",
                      headers: { "Content-Type": "application/json" },
                      body: JSON.stringify({ pharmacy_id: pharmacy.id })
                    });
                    const data = await res.json();
                    if (data.checkout_url) {
                      window.location.href = data.checkout_url;
                    } else {
                      alert("Fehler: " + (data.error || "Unbekannter Fehler"));
                    }
                  } catch (e: any) {
                    alert("Fehler beim Starten des Checkouts: " + e.message);
                  }
                }}
                className="bg-[#0082C8] hover:bg-[#006A9C] text-white"
              >
                Kostenpflichtig abonnieren
              </Button>
            </div>
          )}
        </CardContent>
        <CardFooter className="border-t border-slate-100 flex justify-between p-6">
          <Button variant="ghost" onClick={handleLogout} className="gap-2 text-slate-600 cursor-pointer">
            <LogOut className="w-4 h-4" />
            Abmelden
          </Button>
          <Button variant="outline" onClick={fetchPharmacyData} className="text-slate-600 cursor-pointer">
            Status aktualisieren
          </Button>
        </CardFooter>
      </Card>
    </div>
  );
}
