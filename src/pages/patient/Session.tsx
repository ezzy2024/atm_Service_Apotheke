import { useState, useRef, useEffect, useCallback } from "react";
import { useNavigate, useParams } from "react-router-dom";
import SignatureCanvas from "react-signature-canvas";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import {
  ShieldAlert,
  Video,
  Stethoscope,
  AlertCircle,
  CheckCircle2,
  CreditCard,
} from "lucide-react";
import { supabase } from "@/lib/supabase";
import { JitsiMeeting } from "@jitsi/react-sdk";

type Step =
  | "consent"
  | "service"
  | "triage-disclaimer"
  | "triage-q1"
  | "triage-q2"
  | "triage-q3"
  | "triage-emergency"
  | "triage-result"
  | "video";

import { ServiceType } from "@/src/types";
import { useMediaPermissions } from "@/src/hooks/useMediaPermissions";
import { useAudioAssistant } from "@/src/hooks/useAudioAssistant";

export default function Session() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [step, setStep] = useState<Step>("consent");
  const [consentId, setConsentId] = useState<string>("");

  // Triage State
  const [triageCategory, setTriageCategory] = useState("");
  const [triageDuration, setTriageDuration] = useState("");
  const [urgency, setUrgency] = useState<"Akut / Dringend" | "Nicht akut" | "">(
    "",
  );

  // Consent State
  const [name, setName] = useState("");
  const [healthInsuranceName, setHealthInsuranceName] = useState("");
  const [ikNumber, setIkNumber] = useState("");
  const [insuranceNumber, setInsuranceNumber] = useState(""); // KVNR
  const [birthDate, setBirthDate] = useState("");
  const [statusField5, setStatusField5] = useState("");

  const [hasNoDevice, setHasNoDevice] = useState(false);
  const [isUrgent, setIsUrgent] = useState(false);
  const [needsHelp, setNeedsHelp] = useState(false);
  const [dsgvoAccepted, setDsgvoAccepted] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const logTelemetryEvent = (eventType: string) => {
    const pharmacyId = sessionStorage.getItem("byod_pharmacy_id");
    if (!pharmacyId || !id) return;
    fetch("/api/kiosk/telemetry", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ session_id: id, event_type: eventType, pharmacy_id: pharmacyId })
    }).catch(console.error);
  };

  const sigCanvas = useRef<SignatureCanvas>(null);

  const { hasPermissions, error: mediaError } = useMediaPermissions();
  const [isReconnecting, setIsReconnecting] = useState(false);
  const [jitsiDomain] = useState(import.meta.env.VITE_JITSI_DOMAIN || "meet.jit.si");

  // Audio Assistant
  const { speak } = useAudioAssistant();

  useEffect(() => {
    switch (step) {
      case "consent":
        speak("Schritt 1. Einverständniserklärung. Bitte lesen Sie Ihre Gesundheitskarte ein, oder geben Sie Ihre Daten manuell ein.");
        break;
      case "service":
        speak("Schritt 2. Leistungsauswahl. Möchten Sie nur eine Ersteinschätzung, oder zusätzlich eine ärztliche Videosprechstunde?");
        break;
      case "triage-disclaimer":
        speak("Achtung. Dies ist kein Notrufsystem. Bei akuter Lebensgefahr wählen Sie bitte sofort die 112.");
        break;
      case "triage-q1":
        speak("Frage 1. Welche Beschwerden haben Sie aktuell?");
        break;
      case "triage-q2":
        speak("Frage 2. Seit wann bestehen diese Beschwerden?");
        break;
      case "triage-q3":
        speak("Frage 3. Wie stark schätzen Sie die Dringlichkeit ein?");
        break;
      case "video":
        speak("Die Videosprechstunde wird gestartet. Bitte warten Sie auf den Arzt.");
        break;
    }
    
    // Log telemetry for step change
    const eventName = step === "finished" ? "SESSION_COMPLETED" : `STEP_${step.toUpperCase().replace("-", "_")}`;
    logTelemetryEvent(eventName);
  }, [step, speak]);

  const isIkValid = /^\d{9}$/.test(ikNumber);
  const isKvnrValid = /^[A-Z]\d{9}$/.test(insuranceNumber);
  const isStatusValid = /^\d{5}$/.test(statusField5);

  const isFormValid =
    name.trim() !== "" &&
    healthInsuranceName.trim() !== "" &&
    birthDate !== "" &&
    isIkValid &&
    isKvnrValid &&
    isStatusValid &&
    (hasNoDevice || isUrgent || needsHelp);

  const saveBillingRecord = async (type: ServiceType) => {
    if (!consentId) return;

    // In a real scenario, the Kiosk would authenticate via a service account and the pharmacist's name
    // would be tied to the current active shift or passed from the admin panel that launched the kiosk.
    // For demo purposes, we read it from localStorage.
    const pharmacistName =
      localStorage.getItem("demo_pharmacist_name") || "Apotheker";
    const pharmacyId = sessionStorage.getItem("byod_pharmacy_id");

    try {
      const response = await fetch("/api/kiosk/billing", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          pharmacy_id: pharmacyId,
          consent_id: consentId,
          service_type: type,
          date_of_service: new Date().toISOString().split("T")[0],
          amount: 30.0,
          sonderkennzeichen:
            type === "triage_only"
              ? "19816313"
              : type === "video_only"
                ? "19816336"
                : "19816342",
          executed_by_pharmacist_name: pharmacistName,
        }),
      });

      if (!response.ok) {
        const errData = await response.json();
        throw new Error(errData.error || "Failed to save billing record");
      }

      const data = await response.json();
      const billingId = data.billing_id;

      // Trigger PDF report generation silently in the background
      fetch("/api/kiosk/generate-report", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          pharmacy_id: pharmacyId,
          consent_id: consentId,
          billing_id: billingId,
          triage_data: {
            symptoms: triageCategory,
            duration: triageDuration,
            urgency: urgency,
          }
        })
      }).catch(err => {
        console.error("Failed to generate clinical report silently:", err);
      });

      // Supabase Realtime will automatically notify the Admin Dashboard
      // because we just inserted a new billing_record
    } catch (e) {
      console.error(e);
    }
  };

  const handleStartVideoAfterTriage = () => {
    saveBillingRecord("triage_and_video");
    setStep("video");
  };

  const handleFinishAfterTriage = () => {
    saveBillingRecord("triage_only");
    terminateSession();
  };

  const terminateSession = (isIdleReset: boolean = false) => {
    if (isIdleReset) {
      logTelemetryEvent("SESSION_ABORTED");
    } else {
      logTelemetryEvent("SESSION_COMPLETED");
    }

    // Redirect to patient standby/landing
    navigate(`/patient/start?pharmacy_id=${sessionStorage.getItem("byod_pharmacy_id") || ""}`, { replace: true });
  };

  const handleConsentSubmit = async () => {
    if (
      !name ||
      !healthInsuranceName ||
      !ikNumber ||
      !insuranceNumber ||
      !birthDate ||
      !statusField5 ||
      !dsgvoAccepted ||
      (localStorage.getItem('skip_signature') !== 'true' && sigCanvas.current?.isEmpty())
    ) {
      alert(
        "Bitte füllen Sie alle Felder aus, stimmen Sie der DSGVO zu und unterschreiben Sie das Dokument.",
      );
      return;
    }

    if (ikNumber.length !== 9) {
      alert("Kostenträgerkennung (IK) muss exakt 9 Ziffern lang sein.");
      return;
    }

    if (insuranceNumber.length !== 10) {
      alert("Versichertennummer (KVNR) muss exakt 10 Zeichen lang sein.");
      return;
    }

    if (statusField5.length !== 5) {
      alert("Statusfeld muss exakt 5 Zeichen lang sein (ohne 83).");
      return;
    }

    if (!hasNoDevice && !isUrgent && !needsHelp) {
      alert("Bitte wählen Sie mindestens einen Anspruchsgrund aus.");
      return;
    }

    setIsSaving(true);

    try {
      const isSkipped = localStorage.getItem('skip_signature') === 'true';
      const signatureBlob = isSkipped 
        ? "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=" // dummy pixel
        : (sigCanvas.current?.getCanvas().toDataURL("image/png") || "");
      const fullStatusField = statusField5 + "83";
      const pharmacyId = sessionStorage.getItem("byod_pharmacy_id");

      // Save to Backend Proxy
      const response = await fetch("/api/kiosk/consent", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          pharmacy_id: pharmacyId,
          patient_name: name,
          health_insurance_name: healthInsuranceName,
          health_insurance_number: insuranceNumber,
          ik_number: ikNumber,
          ik_nummer: ikNumber, // Support backend expecting ik_nummer
          birth_date: birthDate,
          status_field: fullStatusField,
          signature_blob: signatureBlob,
        }),
      });

      if (!response.ok) {
        const errData = await response.json();
        throw new Error(errData.error || "Failed to save consent");
      }

      const data = await response.json();

      if (data && data.consent_id) {
        setConsentId(data.consent_id);
      } else if (data && data.id) {
        setConsentId(data.id);
      } else {
        setConsentId(crypto.randomUUID());
      }

      setStep("service");
    } catch (e) {
      console.error(e);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="flex flex-col h-screen overflow-hidden relative">

      {/* Linear Progress Bar */}
      {step !== "triage-emergency" && step !== "video" && (
        <div className="w-full h-4 bg-slate-200">
          <div 
            className="h-full bg-red-600 transition-all duration-500 ease-out"
            style={{ 
              width: step === "consent" ? "33%" : 
                     step === "service" ? "66%" : 
                     "100%" 
            }}
          />
        </div>
      )}

      {/* Kiosk Header */}
      {step !== "triage-emergency" && step !== "video" && (
        <div className="p-6 border-b border-slate-200 flex items-center justify-between bg-slate-50 shrink-0">
          <div>
            <h2 className="text-xl font-bold text-slate-800">
              {step === "consent"
                ? "Schritt 1: Einverständniserklärung"
                : step === "service"
                  ? "Schritt 2: Leistungsauswahl"
                  : "Schritt 3: Ersteinschätzungsverfahren (SmED)"}
            </h2>
            {step.startsWith("triage-q") && (
              <p className="text-sm font-medium text-slate-500 mt-1">
                Schritt{" "}
                {step === "triage-q1" ? "1" : step === "triage-q2" ? "2" : "3"}{" "}
                von 3
              </p>
            )}
          </div>
          <Button
            variant="destructive"
            onClick={() => terminateSession(false)}
            className="bg-red-600 hover:bg-red-700 text-xl px-8 h-20 rounded-xl font-bold border-4 border-slate-900"
          >
            Sitzung abbrechen
          </Button>
        </div>
      )}

      {/* Main Content Area */}
      <div className="flex-1 overflow-auto p-8 relative">
        {step === "consent" && (
          <div className="max-w-2xl mx-auto space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500">
            <div className="bg-red-50 text-red-900 p-6 rounded-xl border border-red-200">
              <div className="flex gap-4">
                <ShieldAlert className="w-8 h-8 text-red-600 shrink-0" />
                <div>
                  <h3 className="font-bold text-lg mb-2">
                    Vereinbarung zur assistierten Telemedizin
                  </h3>
                  <p className="text-sm leading-relaxed">
                    Ich willige in die Verarbeitung meiner Daten im Rahmen der
                    assistierten Inanspruchnahme einer ambulanten
                    telemedizinischen Leistung gegenüber der Apotheke ein. Diese
                    Vereinbarung wird für 4 Jahre in der Apotheke aufbewahrt.
                  </p>
                </div>
              </div>
            </div>

            <div className="flex justify-between items-center mb-2">
              <h3 className="font-semibold text-slate-800 text-lg">
                Patientendaten
              </h3>
            </div>
            <div className="space-y-4">
              <input
                type="text"
                placeholder="Vollständiger Name"
                className="w-full p-4 text-lg border border-slate-900 rounded-lg focus:ring-2 focus:ring-red-600 outline-none"
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <input
                    type="text"
                    placeholder="Krankenkasse Name"
                    className="w-full p-4 text-lg border border-slate-900 rounded-lg focus:ring-2 focus:ring-red-600 outline-none"
                    value={healthInsuranceName}
                    onChange={(e) => setHealthInsuranceName(e.target.value)}
                  />
                </div>
                <div>
                  <input
                    type="text"
                    placeholder="Kostenträgerkennung (IK - 9 Ziffern)"
                    className={`w-full p-4 text-lg border rounded-lg focus:ring-2 focus:ring-red-600 outline-none ${ikNumber && !isIkValid ? "border-red-500 bg-red-50" : "border-slate-900"}`}
                    value={ikNumber}
                    maxLength={9}
                    onChange={(e) => setIkNumber(e.target.value)}
                  />
                  {ikNumber && !isIkValid && (
                    <p className="text-red-500 text-sm mt-1">
                      Muss exakt 9 Ziffern enthalten.
                    </p>
                  )}
                </div>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <input
                    type="text"
                    placeholder="Versichertennummer (KVNR - 1 Buchstabe, 9 Ziffern)"
                    className={`w-full p-4 text-lg border rounded-lg focus:ring-2 focus:ring-red-600 outline-none ${insuranceNumber && !isKvnrValid ? "border-red-500 bg-red-50" : "border-slate-900"}`}
                    value={insuranceNumber}
                    maxLength={10}
                    onChange={(e) =>
                      setInsuranceNumber(e.target.value.toUpperCase())
                    }
                  />
                  {insuranceNumber && !isKvnrValid && (
                    <p className="text-red-500 text-sm mt-1">
                      Muss 1 Großbuchstaben und 9 Ziffern enthalten.
                    </p>
                  )}
                </div>
                <div className="relative">
                  <label className="absolute -top-2 left-3 bg-white px-1 text-xs text-slate-500 font-medium">
                    Geburtsdatum
                  </label>
                  <input
                    type="date"
                    className="w-full p-4 text-lg border border-slate-900 rounded-lg focus:ring-2 focus:ring-red-600 outline-none"
                    value={birthDate}
                    onChange={(e) => setBirthDate(e.target.value)}
                  />
                </div>
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <input
                    type="text"
                    placeholder="Statusfeld (erste 5 Ziffern von eGK)"
                    className={`flex-1 p-4 text-lg border rounded-lg focus:ring-2 focus:ring-red-600 outline-none ${statusField5 && !isStatusValid ? "border-red-500 bg-red-50" : "border-slate-900"}`}
                    value={statusField5}
                    maxLength={5}
                    onChange={(e) => setStatusField5(e.target.value)}
                  />
                  <div className="p-4 bg-slate-100 border border-slate-900 rounded-lg text-lg text-slate-500 font-mono">
                    83
                  </div>
                </div>
                {statusField5 && !isStatusValid && (
                  <p className="text-red-500 text-sm mt-1">
                    Statusfeld muss exakt 5 Ziffern enthalten.
                  </p>
                )}
              </div>
            </div>

            <div className="space-y-4">
              <h3 className="font-semibold text-slate-800 text-lg">
                Anspruchsvoraussetzungen
              </h3>
              <p className="text-sm text-slate-600">
                Bitte wählen Sie mindestens einen zutreffenden Grund:
              </p>

              <label className="flex items-center gap-4 p-4 border border-slate-200 rounded-lg cursor-pointer hover:bg-slate-50 transition-colors">
                <input
                  type="checkbox"
                  className="w-6 h-6 accent-red-600"
                  checked={hasNoDevice}
                  onChange={(e) => setHasNoDevice(e.target.checked)}
                />
                <span className="text-lg">
                  Ich verfüge über kein geeignetes digitales Endgerät
                </span>
              </label>

              <label className="flex items-center gap-4 p-4 border border-slate-200 rounded-lg cursor-pointer hover:bg-slate-50 transition-colors">
                <input
                  type="checkbox"
                  className="w-6 h-6 accent-red-600"
                  checked={isUrgent}
                  onChange={(e) => setIsUrgent(e.target.checked)}
                />
                <span className="text-lg">
                  Dringender Fall (eigenes Gerät kann nicht benutzt werden)
                </span>
              </label>

              <label className="flex items-center gap-4 p-4 border border-slate-200 rounded-lg cursor-pointer hover:bg-slate-50 transition-colors">
                <input
                  type="checkbox"
                  className="w-6 h-6 accent-red-600"
                  checked={needsHelp}
                  onChange={(e) => setNeedsHelp(e.target.checked)}
                />
                <span className="text-lg">
                  Ich benötige praktische oder technische Hilfestellung
                </span>
              </label>
            </div>

            <div className="space-y-4">
              <h3 className="font-semibold text-slate-800 text-lg">
                Datenschutz (DSGVO)
              </h3>
              <label className="flex items-start gap-4 p-4 border border-red-200 bg-red-50/50 rounded-lg cursor-pointer transition-colors">
                <input
                  type="checkbox"
                  className="w-6 h-6 mt-1 accent-red-600"
                  checked={dsgvoAccepted}
                  onChange={(e) => setDsgvoAccepted(e.target.checked)}
                />
                <span className="text-lg text-slate-700 leading-snug">
                  Ich willige ausdrücklich in die Verarbeitung meiner Gesundheitsdaten zur Durchführung der pharmazeutischen Videosprechstunde ein. Die <a href="#" className="text-red-600 underline font-medium" onClick={(e) => { e.preventDefault(); alert("Hier öffnet sich die vollständige Datenschutzerklärung der Apotheke (PDF/Modal)."); }}>Datenschutzerklärung</a> habe ich zur Kenntnis genommen.
                </span>
              </label>
            </div>

            <div className="space-y-4">
              <div className="flex justify-between items-center">
                <h3 className="font-semibold text-slate-800 text-lg">
                  Ihre digitale Unterschrift
                </h3>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => sigCanvas.current?.clear()}
                  className="text-slate-500"
                >
                  Löschen
                </Button>
              </div>
              <div className="border-2 border-slate-900 rounded-xl bg-white overflow-hidden">
                <SignatureCanvas
                  ref={sigCanvas}
                  penColor="black"
                  canvasProps={{ className: "w-full h-48" }}
                />
              </div>
            </div>

            <Button
              className="w-full bg-red-600 hover:bg-red-700 text-white py-8 text-xl font-bold rounded-xl disabled:opacity-50 disabled:cursor-not-allowed"
              onClick={handleConsentSubmit}
              disabled={isSaving || !isFormValid}
            >
              {isSaving ? "Wird gespeichert..." : "Zustimmen & Fortfahren"}
            </Button>
          </div>
        )}

        {step === "service" && (
          <div className="max-w-4xl mx-auto h-full flex flex-col items-center justify-center space-y-12 animate-in fade-in zoom-in-95 duration-500">
            <div className="text-center space-y-4">
              <CheckCircle2 className="w-16 h-16 text-green-500 mx-auto" />
              <h2 className="text-3xl font-bold text-slate-800">
                Zustimmung erfolgreich
              </h2>
              <p className="text-xl text-slate-600">
                Bitte wählen Sie nun die gewünschte Leistung aus.
              </p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-8 w-full">
              <Card className="hover:shadow-xl transition-all cursor-pointer border-2 hover:border-red-600 group">
                <CardContent className="p-8 flex flex-col items-center text-center space-y-6">
                  <div className="w-20 h-20 bg-red-50 text-red-600 rounded-full flex items-center justify-center group-hover:scale-110 transition-transform">
                    <Stethoscope className="w-10 h-10" />
                  </div>
                  <div>
                    <h3 className="text-2xl font-bold text-slate-800 mb-2">
                      Ersteinschätzungsverfahren
                    </h3>
                    <p className="text-slate-600">
                      Strukturierte medizinische Ersteinschätzung (SmED) zur
                      Dringlichkeitsprüfung.
                    </p>
                  </div>
                  <Button
                    className="w-full text-lg py-6 bg-slate-100 text-slate-900 hover:bg-slate-200 mt-auto"
                    onClick={() => setStep("triage-disclaimer")}
                  >
                    Workflow starten
                  </Button>
                </CardContent>
              </Card>

              <Card className="hover:shadow-xl transition-all cursor-pointer border-2 hover:border-red-600 group">
                <CardContent className="p-8 flex flex-col items-center text-center space-y-6">
                  <div className="w-20 h-20 bg-red-50 text-red-600 rounded-full flex items-center justify-center group-hover:scale-110 transition-transform">
                    <Video className="w-10 h-10" />
                  </div>
                  <div>
                    <h3 className="text-2xl font-bold text-slate-800 mb-2">
                      Videosprechstunde
                    </h3>
                    <p className="text-slate-600">
                      Direkter Kontakt mit einer Apothekerin oder einem Apotheker über
                      gesicherte Videoverbindung.
                    </p>
                  </div>
                  <Button
                    className="w-full text-lg py-6 bg-red-600 text-white hover:bg-red-700 mt-auto"
                    onClick={() => {
                      saveBillingRecord("video_only");
                      setStep("video");
                    }}
                  >
                    Sprechstunde starten
                  </Button>
                </CardContent>
              </Card>
            </div>

            <div className="bg-amber-50 border border-amber-200 p-6 rounded-xl flex items-start gap-4 max-w-2xl w-full">
              <AlertCircle className="w-6 h-6 text-amber-600 shrink-0 mt-0.5" />
              <div>
                <p className="font-medium text-amber-900">Wichtiger Hinweis</p>
                <p className="text-amber-800 text-sm mt-1">
                  Wenn Sie die Sitzung beendet haben, drücken Sie bitte auf den
                  roten "Sitzung beenden" Knopf oben rechts, um alle
                  persönlichen Daten vom Gerät zu löschen.
                </p>
              </div>
            </div>
          </div>
        )}

        {step === "triage-disclaimer" && (
          <div className="max-w-3xl mx-auto h-full flex flex-col items-center justify-center space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500">
            <div className="bg-amber-50 border-2 border-amber-500 p-8 rounded-2xl w-full text-center space-y-6">
              <AlertCircle className="w-20 h-20 text-amber-600 mx-auto" />
              <h2 className="text-3xl font-bold text-amber-900 uppercase">Wichtiger Hinweis</h2>
              <p className="text-xl text-amber-800 font-medium">
                Dieses System führt keine medizinische Diagnose durch und ersetzt keinen Arztbesuch in Notfällen.
              </p>
              <div className="bg-white p-6 rounded-xl border border-amber-200">
                <p className="text-lg text-slate-700">
                  Bei akuter Lebensgefahr oder schweren Symptomen (z. B. Brustschmerzen, Atemnot, Lähmungserscheinungen) wählen Sie sofort den Notruf 112.
                </p>
                <p className="text-lg text-slate-700 mt-4">
                  Die anschließende Vorfilterung dient ausschließlich der Vorbereitung des Gesprächs mit dem Apotheker.
                </p>
              </div>
            </div>
            
            <Button
              className="w-full bg-red-600 hover:bg-red-700 text-white py-8 text-xl font-bold rounded-xl"
              onClick={() => setStep("triage-q1")}
            >
              Ich habe verstanden und bestätige
            </Button>
          </div>
        )}

        {step === "triage-q1" && (
          <div className="max-w-4xl mx-auto space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500 text-center">
            <h2 className="text-3xl font-bold text-slate-800">
              Frage 1: Symptom-Kategorie
            </h2>
            <p className="text-xl text-slate-600">
              Bitte wählen Sie die Art Ihrer Hauptbeschwerden:
            </p>
            <div className="grid grid-cols-2 gap-6">
              {[
                "Akuter Schmerz",
                "Atemwegsbeschwerden",
                "Hautveränderungen",
                "Chronische Beschwerden / Kontrolle",
                "Allgemeines Unwohlsein",
              ].map((cat) => (
                <Button
                  key={cat}
                  className="w-full min-h-[160px] text-2xl h-auto whitespace-normal bg-white border-4 border-slate-900 h-20 text-xl font-bold hover:border-red-600 hover:bg-red-50"
                  variant="outline"
                  onClick={() => {
                    setTriageCategory(cat);
                    setStep("triage-q2");
                  }}
                >
                  {cat}
                </Button>
              ))}
            </div>
          </div>
        )}

        {step === "triage-q2" && (
          <div className="max-w-4xl mx-auto space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500 text-center">
            <h2 className="text-3xl font-bold text-slate-800">
              Frage 2: Akute Vitalgefährdung
            </h2>
            <p className="text-xl text-slate-600">
              Leiden Sie unter einem der folgenden Symptome?
            </p>
            <div className="bg-red-50 text-red-900 p-6 rounded-xl border border-red-200 text-left my-6">
              <ul className="list-disc pl-6 text-xl space-y-2 font-medium">
                <li>Starke Brustenge oder Brustschmerzen</li>
                <li>Schwere Atemnot</li>
                <li>Plötzliche Desorientierung oder Verwirrtheit</li>
              </ul>
            </div>
            <div className="grid grid-cols-2 gap-6">
              <Button
                className="w-full min-h-[160px] text-2xl h-auto whitespace-normal bg-red-600 hover:bg-red-700 text-white font-bold"
                onClick={() => setStep("triage-emergency")}
              >
                JA,
                <br />
                diese Symptome liegen vor
              </Button>
              <Button
                className="w-full min-h-[160px] text-2xl h-auto whitespace-normal bg-red-600 hover:bg-red-700 text-white font-bold"
                onClick={() => setStep("triage-q3")}
              >
                NEIN,
                <br />
                keines dieser Symptome
              </Button>
            </div>
          </div>
        )}

        {step === "triage-emergency" && (
          <div className="fixed inset-0 z-50 bg-red-600 text-white flex flex-col items-center justify-center p-8 text-center">
            <AlertCircle className="w-48 h-48 mb-12 animate-pulse" />
            <h1 className="text-6xl font-black uppercase tracking-tight mb-8">
              Akuter Notfall
            </h1>
            <p className="text-4xl font-bold leading-relaxed max-w-4xl">
              Bitte rufen Sie sofort den Notruf 112 an oder wenden Sie sich direkt an das Apothekenpersonal.
            </p>
          </div>
        )}

        {step === "triage-q3" && (
          <div className="max-w-4xl mx-auto space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500 text-center">
            <h2 className="text-3xl font-bold text-slate-800">
              Frage 3: Dauer der Beschwerden
            </h2>
            <p className="text-xl text-slate-600">
              Seit wann bestehen Ihre Hauptbeschwerden in der aktuellen Form?
            </p>
            <div className="grid grid-cols-2 gap-6">
              {["Weniger als 24 Stunden", "Mehrere Tage", "Mehrere Wochen"].map(
                (dur) => (
                  <Button
                    key={dur}
                    className="w-full min-h-[160px] text-2xl h-auto whitespace-normal bg-white border-4 border-slate-900 h-20 text-xl font-bold hover:border-red-600 hover:bg-red-50"
                    variant="outline"
                    onClick={() => {
                      setTriageDuration(dur);
                      const isAcute =
                        triageCategory === "Akuter Schmerz" ||
                        triageCategory === "Atemwegsbeschwerden" ||
                        dur === "Weniger als 24 Stunden";
                      setUrgency(isAcute ? "Akut / Dringend" : "Nicht akut");
                      setStep("triage-result");
                    }}
                  >
                    {dur}
                  </Button>
                ),
              )}
            </div>
          </div>
        )}

        {step === "triage-result" && (
          <div className="max-w-3xl mx-auto space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500 text-center">
            <CheckCircle2 className="w-20 h-20 text-green-500 mx-auto" />
            <h2 className="text-4xl font-bold text-slate-800">
              Ersteinschätzung Abgeschlossen
            </h2>

            <div className="bg-slate-100 p-8 rounded-2xl border border-slate-200">
              <p className="text-lg text-slate-500 uppercase tracking-widest font-semibold mb-2">
                Ermittelte Dringlichkeitsstufe
              </p>
              <p
                className={`text-4xl font-black ${urgency === "Akut / Dringend" ? "text-amber-600" : "text-red-600"}`}
              >
                {urgency}
              </p>
            </div>

            <div className="text-xl text-slate-700 leading-relaxed mb-8">
              {urgency === "Akut / Dringend" ? (
                <p>
                  Eine ärztliche Abklärung ist zeitnah empfohlen. Sie können nun
                  direkt aus der Apotheke eine Videosprechstunde mit einem Apotheker
                  starten.
                </p>
              ) : (
                <p>
                  Eine sofortige ärztliche Abklärung ist aktuell nicht zwingend
                  erforderlich. Bitte vereinbaren Sie bei Bedarf einen regulären
                  Termin bei Ihrem Hausarzt oder Spezialisten.
                </p>
              )}
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mt-12">
              {urgency === "Akut / Dringend" ? (
                <>
                  <Button
                    className="w-full py-8 text-xl h-auto whitespace-normal bg-red-600 hover:bg-red-700 text-white font-bold"
                    onClick={handleStartVideoAfterTriage}
                  >
                    Videosprechstunde starten
                  </Button>
                  <Button
                    className="w-full py-8 text-xl h-auto whitespace-normal bg-white border-2 border-slate-900 text-slate-700 hover:bg-slate-50"
                    variant="outline"
                    onClick={handleFinishAfterTriage}
                  >
                    Sitzung beenden
                  </Button>
                </>
              ) : (
                <Button
                  className="w-full py-8 text-2xl h-auto whitespace-normal bg-red-600 hover:bg-red-700 text-white font-bold md:col-span-2"
                  onClick={handleFinishAfterTriage}
                >
                  Sitzung beenden
                </Button>
              )}
            </div>
          </div>
        )}

        {step === "video" && (
          <div className="max-w-6xl mx-auto h-[80vh] flex flex-col space-y-4 animate-in fade-in zoom-in duration-500">
            <div className="flex items-center justify-between">
              <h2 className="text-3xl font-bold text-slate-800">
                Live Videosprechstunde
              </h2>
              <Button
                variant="destructive"
                className="font-bold py-6 px-8"
                onClick={terminateSession}
              >
                Sitzung beenden
              </Button>
            </div>
            <div className="flex-1 bg-black rounded-2xl overflow-hidden border-4 border-slate-200 relative">
              {hasPermissions === false && (
                <div className="absolute inset-0 z-50 flex items-center justify-center bg-slate-900 text-white p-6 text-center">
                  <div>
                    <AlertCircle className="w-16 h-16 text-red-500 mx-auto mb-4" />
                    <h3 className="text-2xl font-bold mb-2">Kamera/Mikrofon blockiert</h3>
                    <p className="text-slate-300">{mediaError || "Bitte wenden Sie sich an das Personal."}</p>
                  </div>
                </div>
              )}
              {isReconnecting && (
                <div className="absolute inset-0 z-40 flex items-center justify-center bg-black/80 text-white backdrop-blur-sm">
                  <div className="text-center">
                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-white mx-auto mb-4"></div>
                    <p className="text-lg">Verbindung unterbrochen – wir verbinden Sie neu...</p>
                  </div>
                </div>
              )}
              {hasPermissions !== false && (
                <JitsiMeeting
                  domain={jitsiDomain}
                  roomName={`atm-service-apotheke-${consentId}`}
                  configOverwrite={{
                    startWithAudioMuted: false,
                    startWithVideoMuted: false,
                    prejoinPageEnabled: false,
                    disableModeratorIndicator: true,
                    resolution: 480,
                    constraints: {
                      video: {
                        height: { ideal: 480, max: 480, min: 240 },
                        aspectRatio: 16 / 9,
                        frameRate: { ideal: 24, max: 30 }
                      }
                    },
                    p2p: { enabled: false }, // Force through JVB for NAT traversal
                    disableAudioLevels: false
                  }}
                  interfaceConfigOverwrite={{
                    DISABLE_JOIN_LEAVE_NOTIFICATIONS: true,
                    SHOW_CHROME_EXTENSION_BANNER: false,
                  }}
                  userInfo={{
                    displayName: name || 'Patient'
                  }}
                  onApiReady={(externalApi) => {
                    externalApi.addListener('videoConferenceLeft', () => terminateSession());
                    externalApi.addListener('videoConferenceJoined', () => setIsReconnecting(false));
                    externalApi.addListener('participantLeft', () => setIsReconnecting(false));
                  }}
                  getIFrameRef={(iframeRef) => {
                    iframeRef.style.height = '100%';
                    iframeRef.style.width = '100%';
                    iframeRef.style.border = '0';
                  }}
                />
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
