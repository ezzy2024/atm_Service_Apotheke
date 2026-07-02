import { useState, useEffect, useRef } from "react";
import { format, addMinutes } from "date-fns";
import { de } from "date-fns/locale";
import { useNavigate } from "react-router-dom";
import {
  Calendar as CalendarIcon,
  FileSignature,
  Receipt,
  Download,
  FileText,
  CheckCircle2,
  Plus,
  MonitorSmartphone,
  Link2Off,
} from "lucide-react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { supabase } from "@/lib/supabase";
import {
  ConsentAgreement,
  BillingRecord,
  Appointment,
  ServiceType,
} from "@/src/types";

import { JitsiMeeting } from "@jitsi/react-sdk";
import { toast } from "sonner";

import FullCalendar from "@fullcalendar/react";
import dayGridPlugin from "@fullcalendar/daygrid";
import timeGridPlugin from "@fullcalendar/timegrid";
import interactionPlugin from "@fullcalendar/interaction";
import deLocale from "@fullcalendar/core/locales/de";

// Mock Data fallback
const MOCK_APPOINTMENTS: Appointment[] = [
  {
    id: "1",
    patient_name: "Max Mustermann",
    start_time: new Date().toISOString(),
    end_time: new Date(Date.now() + 1800000).toISOString(),
    status: "scheduled",
  },
  {
    id: "2",
    patient_name: "Erika Musterfrau",
    start_time: new Date(Date.now() + 3600000).toISOString(),
    end_time: new Date(Date.now() + 5400000).toISOString(),
    status: "scheduled",
  },
];

const MOCK_CONSENTS: ConsentAgreement[] = [
  {
    id: "1",
    patient_name: "Johannes Schmidt",
    health_insurance_name: "AOK",
    health_insurance_number: "A123456789",
    ik_number: "123456789",
    birth_date: "1980-01-01",
    status_field: "1000083",
    signed_date: new Date().toISOString(),
    signature_blob: "",
  },
];

export default function Dashboard() {
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [agreements, setAgreements] = useState<ConsentAgreement[]>([]);
  const [billingRecords, setBillingRecords] = useState<BillingRecord[]>([]);
  const [terminals, setTerminals] = useState<any[]>([]);
  const [isPairingLoading, setIsPairingLoading] = useState(false);
  const [pairingCode, setPairingCode] = useState<string | null>(null);
  const [activeVideoCall, setActiveVideoCall] = useState<string | null>(null);
  const [telemetry, setTelemetry] = useState<any>(null);
  const [billingPage, setBillingPage] = useState(1);
  const [billingTotal, setBillingTotal] = useState(0);
  const [dateRange, setDateRange] = useState({ start: '', end: '' });
  const [exportingFormat, setExportingFormat] = useState<"json" | "csv" | "pdf" | null>(null);
  const [isAddEventOpen, setIsAddEventOpen] = useState(false);
  const [newPassword, setNewPassword] = useState("");
  const [jitsiDomain] = useState(import.meta.env.VITE_JITSI_DOMAIN || "meet.jit.si");
  const [isReconnecting, setIsReconnecting] = useState(false);
  const [authorizedPharmacies, setAuthorizedPharmacies] = useState<string[]>([]);
  const [selectedPharmacyId, setSelectedPharmacyId] = useState<string | null>(null);
  
  const [newEvent, setNewEvent] = useState({
    title: "",
    patient_name: "",
    start: new Date(),
    end: addMinutes(new Date(), 30),
  });

  const knownBillingIds = useRef<Set<string>>(new Set());
  const isFirstLoad = useRef(true);

  useEffect(() => {
    initializeDashboard();
  }, []);

  const initializeDashboard = async () => {
    try {
      const { data: { user } } = await supabase.auth.getUser();
      if (!user) return;
      
      const { data: roles } = await supabase
        .from("user_pharmacy_roles")
        .select("pharmacy_id")
        .eq("user_id", user.id);
        
      const pharmacies = roles?.map(r => r.pharmacy_id) || [];
      setAuthorizedPharmacies(pharmacies);
      
      if (pharmacies.length > 0) {
        fetchData(pharmacies, null);
        setupRealtime(pharmacies);
      }
    } catch (e) {
      console.error("Failed to initialize dashboard:", e);
    }
  };

  const setupRealtime = (pharmacies: string[]) => {
    if (pharmacies.length === 0) return;
    
    // Fallback polling
    const interval = setInterval(() => {
      fetchData(pharmacies, selectedPharmacyId);
    }, 10000);

    // Filter for realtime (if possible, otherwise filter in client)
    const filterString = pharmacies.length === 1 ? `pharmacy_id=eq.${pharmacies[0]}` : undefined;


    // Supabase Realtime Subscription for new video calls
    const channel = supabase
      .channel("custom-insert-channel")
      .on(
        "postgres_changes",
        {
          event: "INSERT",
          schema: "public",
          table: "billing_records",
          filter: filterString,
        },
        (payload) => {
          const newRecord = payload.new;
          if (
            newRecord.service_type === "video_only" ||
            newRecord.service_type === "triage_and_video"
          ) {
            const isTriage = newRecord.service_type === "triage_and_video";
            toast(
              isTriage
                ? "Patient wartet auf Ersteinschätzung & Video"
                : "Patient wartet auf Videosprechstunde",
              {
                description: "Ein Patient hat gerade am Kiosk eine neue Sprechstunde gestartet.",
                action: {
                  label: "Beitreten",
                  onClick: () => handleJoinVideo(newRecord.consent_id),
                },
                duration: 10000,
              }
            );
            fetchData(pharmacies, selectedPharmacyId);
          }
        }
      )
      .subscribe();

    return () => {
      channel.unsubscribe();
      clearInterval(interval);
    };
  };

  const fetchData = async (pharmacies: string[], selectedId: string | null) => {
    setIsLoading(true);
    const targetPharmacies = selectedId ? [selectedId] : pharmacies;
    if (targetPharmacies.length === 0) {
      setIsLoading(false);
      return;
    }

    try {
      // 1. Consent Agreements
      const { data: consentData } = await supabase
        .from("consent_agreements")
        .select("*")
        .in("pharmacy_id", targetPharmacies)
        .order("created_at", { ascending: false });
      if (consentData) setAgreements(consentData);

      // 2. Billing Records
      let billingQuery = supabase
        .from("billing_records")
        .select("*, consent_agreements(*), pharmacies(name)", { count: "exact" })
        .in("pharmacy_id", targetPharmacies)
        .order("created_at", { ascending: false })
        .range((billingPage - 1) * 10, billingPage * 10 - 1);
        
      if (dateRange.start) billingQuery = billingQuery.gte("created_at", dateRange.start);
      if (dateRange.end) billingQuery = billingQuery.lte("created_at", dateRange.end);

      const { data: billingData, count: billingCount } = await billingQuery;
      
      if (billingData) {
        setBillingRecords(billingData as any);
        setBillingTotal(billingCount || 0);
        isFirstLoad.current = false;
      }

      // 3. Appointments
      const { data: aptData } = await supabase
        .from("appointments")
        .select("*")
        .in("pharmacy_id", targetPharmacies);
      if (aptData) setAppointments(aptData);

      // 4. Telemetry and Terminals removed for BYOD
      setTerminals([]);
      setTelemetry(null);

    } catch (e) {
      console.error("Fetch error:", e);
    }
    setIsLoading(false);
  };

  // Whenever selectedPharmacyId changes, refetch
  useEffect(() => {
    if (authorizedPharmacies.length > 0) {
      fetchData(authorizedPharmacies, selectedPharmacyId);
    }
  }, [selectedPharmacyId, billingPage, dateRange]);

  const getSonderkennzeichen = (type: ServiceType) => {
    switch (type) {
      case "triage_only":
        return "19816313";
      case "video_only":
        return "19816336";
      case "triage_and_video":
        return "19816342";
    }
  };

  const calculateAmount = (dateStr: string) => {
    const date = new Date(dateStr);
    const cutoff = new Date("2027-07-01");
    return date < cutoff ? 30.0 : 25.5;
  };

  const handleGeneratePairingCode = async () => {
    setIsPairingLoading(true);
    setPairingCode(null);
    try {
      const pharmacyId = localStorage.getItem("demo_pharmacy_id") || "d3b07384-d113-4956-a50e-a1c563e4410a";
      const res = await fetch("/api/admin/terminals/pair", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ pharmacy_id: pharmacyId })
      });
      if (!res.ok) throw new Error("Fehler beim Generieren");
      const data = await res.json();
      setPairingCode(data.code);
    } catch (e) {
      toast.error("Fehler beim Generieren des Pairing Codes");
    }
    setIsPairingLoading(false);
  };

  const handleRevokeTerminal = async (id: string) => {
    if (!confirm("Möchten Sie diesem Terminal wirklich den Zugriff entziehen?")) return;
    try {
      const res = await fetch(`/api/admin/terminals/${id}`, { method: "DELETE" });
      if (!res.ok) throw new Error("Fehler beim Widerrufen");
      toast.success("Terminal-Zugriff widerrufen");
      fetchData();
    } catch (e) {
      toast.error("Fehler beim Widerrufen des Terminals");
    }
  };

  const handleJoinVideo = async (consentId: string, pharmacyId: string, recordId: string, currentStatus: string) => {
    try {
      if (currentStatus !== 'in_consultation') {
        // Atomic update to prevent race conditions
        const { data, error } = await supabase
          .from("billing_records")
          .update({ status: "in_consultation" })
          .eq("id", recordId)
          .eq("status", "waiting" as any)
          .select();
          
        if (error) throw error;
        
        if (!data || data.length === 0) {
          toast.error("Patient befindet sich bereits in Behandlung durch einen anderen Apotheker.");
          if (authorizedPharmacies.length > 0) fetchData(authorizedPharmacies, selectedPharmacyId);
          return;
        }
        
        // Also update local state
        setBillingRecords(prev => 
          prev.map(r => r.id === recordId ? { ...r, status: "in_consultation" } : r)
        );
      }
      
      // Join the call
      const roomName = `${pharmacyId}-${consentId}`;
      setActiveVideoCall(roomName);
    } catch (e) {
      console.error("Failed to update status", e);
      toast.error("Systemfehler beim Starten des Video-Calls.");
    }
  };

  const handleViewReport = async (reportPath: string) => {
    try {
      const response = await fetch(`/api/admin/report-url?report_path=${encodeURIComponent(reportPath)}`);
      if (!response.ok) {
        throw new Error("Fehler beim Abrufen des Berichts-Links");
      }
      const data = await response.json();
      if (data && data.url) {
        window.open(data.url, "_blank");
      } else {
        alert("Bericht konnte nicht geladen werden.");
      }
    } catch (e: any) {
      console.error(e);
      alert(e.message || "Fehler beim Laden des Berichts.");
    }
  };

  const triggerExport = async (format: "json" | "csv" | "pdf") => {
    try {
      setExportingFormat(format);
      const pharmacyId = localStorage.getItem("demo_pharmacy_id") || "d3b07384-d113-4956-a50e-a1c563e4410a";
      const currentMonth = format(new Date(), "yyyy-MM");
      
      const endpoint = format === "json" 
        ? `/api/admin/billing/export/${pharmacyId}?month=${currentMonth}`
        : `/api/admin/billing/export-${format}/${pharmacyId}?month=${currentMonth}`;
        
      const res = await fetch(endpoint);
      if (!res.ok) throw new Error("Fehler beim Erstellen der Abrechnung.");
      
      let blob: Blob;
      let filename = `NNF_Abrechnung_${currentMonth}.${format}`;
      
      if (format === "json") {
        const jsonData = await res.json();
        blob = new Blob([JSON.stringify(jsonData, null, 2)], { type: "application/json" });
        filename = `NNF_Abrechnung_${jsonData.Abrechnungs_Metadaten?.IK_Apotheke}_${currentMonth}.json`;
      } else {
        blob = await res.blob();
        filename = format === "pdf" ? `Sonderbeleg_${currentMonth}.pdf` : `NNF_Abrechnung_${currentMonth}.csv`;
        
        // Try to get filename from Content-Disposition if available
        const disposition = res.headers.get("Content-Disposition");
        if (disposition && disposition.indexOf('filename=') !== -1) {
          const filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
          const matches = filenameRegex.exec(disposition);
          if (matches != null && matches[1]) { 
            filename = matches[1].replace(/['"]/g, '');
          }
        }
      }
      
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.setAttribute("hidden", "");
      a.setAttribute("href", url);
      a.setAttribute("download", filename);
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);
    } catch (e: any) {
      console.error(e);
      alert(e.message || "Export fehlgeschlagen.");
    } finally {
      setExportingFormat(null);
    }
  };

  const handleSelectSlot = (selectInfo: any) => {
    setNewEvent({ title: "", start: selectInfo.start, end: selectInfo.end });
    setIsAddEventOpen(true);
  };

  const handleAddEvent = async () => {
    if (!newEvent.title) return;

    // Simple double-booking check
    const isDoubleBooked = appointments.some((apt) => {
      const aptStart = new Date(apt.start_time);
      const aptEnd = new Date(apt.end_time);
      return newEvent.start < aptEnd && newEvent.end > aptStart;
    });

    if (isDoubleBooked) {
      alert(
        "Dieser Zeitraum ist bereits gebucht. Bitte wählen Sie einen anderen Termin.",
      );
      return;
    }

    const newApt: Appointment = {
      id: crypto.randomUUID(),
      patient_name: newEvent.patient_name || newEvent.title || "Unbekannter Patient",
      start_time: newEvent.start.toISOString(),
      end_time: newEvent.end.toISOString(),
      status: "scheduled",
    };

    setAppointments([...appointments, newApt]);

    // Sync to DB
    try {
      const pharmacyId = localStorage.getItem("demo_pharmacy_id") || "d3b07384-d113-4956-a50e-a1c563e4410a";
      await fetch("/api/admin/appointments", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          pharmacy_id: pharmacyId,
          patient_name: newEvent.patient_name || newEvent.title || "Unbekannter Patient",
          start_time: newEvent.start.toISOString(),
          end_time: newEvent.end.toISOString(),
          status: "scheduled",
        }),
      });
    } catch (e) {
      console.error(e);
    }

    setIsAddEventOpen(false);
  };

  const calendarEvents = appointments.map((apt) => {
    let backgroundColor = "#0082C8";
    if (apt.status === "completed") backgroundColor = "#10B981"; // green
    if (apt.status === "in-progress") backgroundColor = "#F59E0B"; // amber

    return {
      id: apt.id,
      title: apt.patient_name,
      start: new Date(apt.start_time),
      end: new Date(apt.end_time),
      backgroundColor,
      borderColor: backgroundColor,
    };
  });

  // Find if there are any waiting patients (from the last 60 minutes)
  const waitingPatients = billingRecords.filter(r => {
    const isRecent = (new Date().getTime() - new Date(r.date_of_service).getTime()) < 60 * 60 * 1000;
    const isVideoService = r.service_type === "video_only" || r.service_type === "triage_and_video";
    return isRecent && isVideoService;
  });

  return (
    <div className="max-w-6xl mx-auto space-y-8 animate-in fade-in duration-500">
      
      {waitingPatients.length > 0 && (
        <div className="bg-red-500 text-white p-6 rounded-xl shadow-lg flex flex-col md:flex-row items-start md:items-center justify-between gap-4 animate-in slide-in-from-top-4">
          <div className="flex items-center gap-4">
            <div className="bg-white/20 p-3 rounded-full animate-pulse">
              <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M15 14c.2-1 .7-1.7 1.5-2.5 1-.9 1.5-2.2 1.5-3.5A6 6 0 0 0 6 8c0 1 .2 2.2 1.5 3.5.7.9 1.3 1.5 1.5 2.5"/><path d="M9 18h6"/><path d="M10 22h4"/></svg>
            </div>
            <div>
              <h3 className="text-xl font-bold">Patient wartet im Kiosk!</h3>
              <p className="text-red-100 font-medium">{waitingPatients.length} wartende Person(en) auf eine Videosprechstunde.</p>
            </div>
          </div>
          <Button 
            className="bg-white text-red-600 hover:bg-red-50 font-bold text-lg py-6 px-8 shadow-md"
            onClick={() => handleJoinVideo((waitingPatients[0] as any).consent_id || waitingPatients[0].id)}
          >
            Jetzt Beitreten
          </Button>
        </div>
      )}

      <div>
        <h1 className="text-3xl font-bold text-slate-900 tracking-tight">
          Übersicht
        </h1>
        <p className="text-slate-500 mt-2">
          Verwalten Sie hier Termine, Einverständniserklärungen und Abrechnungen
          für die assistierte Telemedizin.
        </p>
      </div>

      {telemetry && (
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8 mt-6">
          <Card className="border-slate-200 shadow-sm">
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-slate-500">Gesamtsitzungen</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-3xl font-bold text-slate-900">{telemetry.total_sessions}</div>
            </CardContent>
          </Card>
          <Card className="border-slate-200 shadow-sm">
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-slate-500">Abgeschlossen</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-3xl font-bold text-emerald-600">{telemetry.completed_sessions}</div>
            </CardContent>
          </Card>
          <Card className="border-slate-200 shadow-sm">
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-slate-500">Abgebrochen</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-3xl font-bold text-red-600">{telemetry.aborted_sessions}</div>
            </CardContent>
          </Card>
          <Card className="border-slate-200 shadow-sm">
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium text-slate-500">Erfolgsquote</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-3xl font-bold text-blue-600">{telemetry.success_rate}%</div>
            </CardContent>
          </Card>
        </div>
      )}

      <Tabs defaultValue="schedule" className="w-full">
        <TabsList className="bg-white border border-slate-200 p-1 rounded-lg h-14 mb-6 shadow-sm">
          <TabsTrigger
            value="schedule"
            className="data-[state=active]:bg-[#0082C8] data-[state=active]:text-white rounded-md px-6 py-2.5 text-sm font-medium transition-all"
          >
            <CalendarIcon className="w-4 h-4 mr-2" />
            Terminkalender
          </TabsTrigger>
          <TabsTrigger
            value="consents"
            className="data-[state=active]:bg-[#0082C8] data-[state=active]:text-white rounded-md px-6 py-2.5 text-sm font-medium transition-all"
          >
            <FileSignature className="w-4 h-4 mr-2" />
            Einverständniserklärungen
          </TabsTrigger>
          <TabsTrigger
            value="billing"
            className="data-[state=active]:bg-[#0082C8] data-[state=active]:text-white rounded-md px-6 py-2.5 text-sm font-medium transition-all"
          >
            <Receipt className="w-4 h-4 mr-2" />
            Abrechnungsgruppen
          </TabsTrigger>
        </TabsList>

        <TabsContent value="schedule" className="mt-0">
          <Card className="border-slate-200 shadow-sm h-[700px] flex flex-col">
            <CardHeader className="flex flex-row items-start justify-between pb-4">
              <div>
                <CardTitle>Geplante aTM-Sitzungen</CardTitle>
                <CardDescription>
                  Termine für die Nutzung des Telemedizin-Raums. Klicken Sie in
                  den Kalender, um einen Termin zu blockieren.
                </CardDescription>
              </div>
            </CardHeader>
            <CardContent className="flex-1 p-4">
              <FullCalendar
                plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
                initialView="timeGridWeek"
                events={calendarEvents}
                selectable={true}
                select={handleSelectSlot}
                height="100%"
                locale={deLocale}
                slotMinTime="08:00:00"
                slotMaxTime="18:00:00"
                allDaySlot={false}
                headerToolbar={{
                  left: "prev,next today",
                  center: "title",
                  right: "dayGridMonth,timeGridWeek,timeGridDay",
                }}
              />
            </CardContent>
          </Card>

          <Dialog open={isAddEventOpen} onOpenChange={setIsAddEventOpen}>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Termin / Blockierung eintragen</DialogTitle>
              </DialogHeader>
              <div className="space-y-4 py-4">
                <div className="space-y-2">
                  <Label>Titel</Label>
                  <Input
                    value={newEvent.title}
                    onChange={(e) =>
                      setNewEvent({ ...newEvent, title: e.target.value })
                    }
                    placeholder="z.B. Wartung oder Termin"
                  />
                </div>
                <div className="grid grid-cols-4 items-center gap-4">
                  <Label className="text-right">Von</Label>
                  <div className="col-span-3 text-sm">
                    {format(newEvent.start, "dd.MM.yyyy HH:mm", { locale: de })}
                  </div>
                </div>
                <div className="grid grid-cols-4 items-center gap-4">
                  <Label className="text-right">Bis</Label>
                  <div className="col-span-3 text-sm">
                    {format(newEvent.end, "dd.MM.yyyy HH:mm", { locale: de })}
                  </div>
                </div>
              </div>
              <DialogFooter>
                <Button
                  variant="outline"
                  onClick={() => setIsAddEventOpen(false)}
                >
                  Abbrechen
                </Button>
                <Button
                  onClick={handleAddEvent}
                  className="bg-[#0082C8] hover:bg-[#006A9C] text-white"
                >
                  Termin speichern
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        </TabsContent>

        <TabsContent value="consents" className="mt-0">
          <Card className="border-slate-200 shadow-sm">
            <CardHeader>
              <CardTitle>Vorliegende Vereinbarungen</CardTitle>
              <CardDescription>
                Digital unterschriebene Einverständniserklärungen (4 Jahre
                Aufbewahrungsfrist).
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="rounded-md border border-slate-200">
                <Table>
                  <TableHeader className="bg-slate-50">
                    <TableRow>
                      <TableHead>Patient</TableHead>
                      <TableHead>Krankenkasse</TableHead>
                      <TableHead>IK</TableHead>
                      <TableHead>KVNR</TableHead>
                      <TableHead>Geburtsdatum</TableHead>
                      <TableHead>Unterschrieben am</TableHead>
                      <TableHead className="text-right">Dokument</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {agreements.map((c) => {
                      const signedDate = new Date(c.signed_date);
                      const expiryDate = new Date(signedDate);
                      expiryDate.setFullYear(expiryDate.getFullYear() + 4);

                      return (
                        <TableRow key={c.id}>
                          <TableCell className="font-medium flex items-center gap-2">
                            <CheckCircle2 className="w-4 h-4 text-green-500" />
                            {c.patient_name}
                          </TableCell>
                          <TableCell>
                            {c.health_insurance_name || "-"}
                          </TableCell>
                          <TableCell className="font-mono text-xs">
                            {c.ik_number || "-"}
                          </TableCell>
                          <TableCell className="font-mono text-xs text-slate-600">
                            {c.health_insurance_number}
                          </TableCell>
                          <TableCell>
                            {c.birth_date
                              ? format(new Date(c.birth_date), "dd.MM.yyyy")
                              : "-"}
                          </TableCell>
                          <TableCell>
                            {format(signedDate, "dd.MM.yyyy")}
                          </TableCell>
                          <TableCell className="text-right">
                            <Button
                              variant="outline"
                              size="sm"
                              className="gap-2"
                            >
                              <FileText className="w-4 h-4" />
                              PDF
                            </Button>
                          </TableCell>
                        </TableRow>
                      );
                    })}
                  </TableBody>
                </Table>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="billing" className="mt-0">
          <Card className="border-slate-200 shadow-sm">
            <CardHeader className="flex flex-row items-center justify-between pb-4">
              <div>
                <CardTitle>Abrechnungsdatensätze (ARZ)</CardTitle>
                <CardDescription>
                  Abgeschlossene Leistungen mit zugeordneten PZN/Leistungscodes für die Abrechnung.
                </CardDescription>
              </div>
              <div className="flex flex-col sm:flex-row items-end sm:items-center gap-4">
                <div className="flex items-center gap-2 bg-slate-50 p-2 rounded-lg border border-slate-200">
                  <Input type="date" className="h-9 w-36" value={dateRange.start} onChange={(e) => { setDateRange(prev => ({...prev, start: e.target.value})); setBillingPage(1); }} />
                  <span className="text-slate-500">-</span>
                  <Input type="date" className="h-9 w-36" value={dateRange.end} onChange={(e) => { setDateRange(prev => ({...prev, end: e.target.value})); setBillingPage(1); }} />
                  <Button variant="secondary" size="sm" onClick={() => fetchData()}>Filter anwenden</Button>
                </div>
                <Button 
                  onClick={() => {
                    const pharmacyId = localStorage.getItem("demo_pharmacy_id") || "d3b07384-d113-4956-a50e-a1c563e4410a";
                    const url = `/api/admin/billing/export?pharmacy_id=${pharmacyId}${dateRange.start ? `&start_date=${dateRange.start}&end_date=${dateRange.end}` : ''}`;
                    window.open(url, "_blank");
                  }} 
                  className="bg-[#0082C8] hover:bg-[#006A9C] text-white h-10"
                >
                  <Download className="w-4 h-4 mr-2" />
                  ARZ / NNF Export (CSV)
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              <div className="rounded-md border border-slate-200">
                <Table>
                  <TableHeader className="bg-slate-50">
                    <TableRow>
                      <TableHead>Datum</TableHead>
                      <TableHead>Standort</TableHead>
                      <TableHead>Patient</TableHead>
                      <TableHead>KVNR</TableHead>
                      <TableHead>Leistungscode</TableHead>
                      <TableHead>Betrag</TableHead>
                      <TableHead className="text-right">Aktionen</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {billingRecords.map((record: any) => (
                      <TableRow key={record.id}>
                        <TableCell className="font-medium text-sm">
                          {format(new Date(record.created_at || record.date_of_service), "dd.MM.yyyy HH:mm", { locale: de })}
                        </TableCell>
                        <TableCell>
                          {record.pharmacies?.name || "-"}
                        </TableCell>
                        <TableCell>
                          {record.consent_agreements?.patient_name || "Unbekannt"}
                        </TableCell>
                        <TableCell>
                          {record.consent_agreements?.health_insurance_number || "-"}
                        </TableCell>
                        <TableCell>
                          <span className="font-mono text-xs bg-slate-100 px-2 py-1 rounded">
                            {record.sonderkennzeichen || getSonderkennzeichen(record.service_type)}
                          </span>
                        </TableCell>
                        <TableCell>
                          {new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(record.amount || calculateAmount(record.date_of_service))}
                        </TableCell>
                        <TableCell className="text-right space-x-2">
                            {(record.service_type === "video_only" || record.service_type === "triage_and_video") && (
                              <Button
                                size="sm"
                                className={`text-xs font-bold py-1 px-3 cursor-pointer ${
                                  record.status === 'in_consultation' 
                                    ? 'bg-amber-500 hover:bg-amber-600 text-white' 
                                    : 'bg-[#0082C8] hover:bg-[#006A9C] text-white'
                                }`}
                                onClick={() => handleJoinVideo((record as any).consent_id || record.id, record.pharmacy_id, record.id, record.status || 'waiting')}
                              >
                                {record.status === 'in_consultation' ? 'Wieder beitreten' : 'Videosprechstunde beitreten'}
                              </Button>
                            )}
                            {record.report_path && (
                              <Button
                                variant="outline"
                                size="sm"
                                className="text-xs font-bold py-1 px-3 border-slate-300 text-slate-700 hover:bg-slate-50 cursor-pointer"
                                onClick={() => handleViewReport(record.report_path)}
                              >
                                Anamnese-Protokoll ansehen
                              </Button>
                            )}
                        </TableCell>
                      </TableRow>
                    ))}
                    {billingRecords.length === 0 && (
                      <TableRow>
                        <TableCell colSpan={6} className="text-center py-8 text-slate-500">
                          Keine Abrechnungsdaten für den gewählten Zeitraum gefunden.
                        </TableCell>
                      </TableRow>
                    )}
                  </TableBody>
                </Table>
              </div>
              
              {/* Pagination Controls */}
              <div className="flex items-center justify-between mt-4">
                <div className="text-sm text-slate-500">
                  Zeige {billingRecords.length} von {billingTotal} Einträgen
                </div>
                <div className="flex gap-2">
                  <Button variant="outline" size="sm" disabled={billingPage === 1} onClick={() => { setBillingPage(p => p - 1); }}>Zurück</Button>
                  <Button variant="outline" size="sm" disabled={billingPage * 10 >= billingTotal} onClick={() => { setBillingPage(p => p + 1); }}>Weiter</Button>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card className="border-slate-200 shadow-sm mt-6">
            <CardHeader>
              <CardTitle>B2B-Abonnement & Rechnungen</CardTitle>
              <CardDescription>
                Verwalten Sie hier Ihr monatliches aTM-Abonnement, aktualisieren Sie Ihre Zahlungsdaten oder laden Sie Rechnungen herunter.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Button
                variant="outline"
                className="gap-2"
                onClick={async () => {
                  try {
                    const pharmacyId = localStorage.getItem("demo_pharmacy_id") || "d3b07384-d113-4956-a50e-a1c563e4410a";
                    const res = await fetch("/api/stripe/portal-session", {
                      method: "POST",
                      headers: { "Content-Type": "application/json" },
                      body: JSON.stringify({ pharmacy_id: pharmacyId })
                    });
                    const data = await res.json();
                    if (data.portal_url) {
                      window.location.href = data.portal_url;
                    } else {
                      alert("Sie haben aktuell kein aktives Stripe-Abonnement oder die API-Keys fehlen (" + (data.error || "Unbekannter Fehler") + ").");
                    }
                  } catch (e: any) {
                    alert("Fehler beim Öffnen des Kundenportals: " + e.message);
                  }
                }}
              >
                Kundenportal öffnen
              </Button>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
      <Dialog open={isAddEventOpen} onOpenChange={setIsAddEventOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Termin / Blockierung eintragen</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label>Titel / Grund</Label>
              <Input
                value={newEvent.title}
                onChange={(e) =>
                  setNewEvent({ ...newEvent, title: e.target.value })
                }
                placeholder="z.B. Wartung, pDL Termin, etc."
              />
            </div>
            <div className="space-y-2">
              <Label>Patientenname (optional)</Label>
              <Input
                value={newEvent.patient_name}
                onChange={(e) =>
                  setNewEvent({ ...newEvent, patient_name: e.target.value })
                }
                placeholder="Name des Patienten"
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Startzeit</Label>
                <Input
                  type="datetime-local"
                  value={format(newEvent.start, "yyyy-MM-dd'T'HH:mm")}
                  onChange={(e) =>
                    setNewEvent({ ...newEvent, start: new Date(e.target.value) })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label>Endzeit</Label>
                <Input
                  type="datetime-local"
                  value={format(newEvent.end, "yyyy-MM-dd'T'HH:mm")}
                  onChange={(e) =>
                    setNewEvent({ ...newEvent, end: new Date(e.target.value) })
                  }
                />
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsAddEventOpen(false)}>
              Abbrechen
            </Button>
            <Button onClick={handleAddEvent}>Speichern</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <TabsContent value="terminals" className="mt-0">
        <Card className="border-slate-200 shadow-sm mb-8">
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <CardTitle>Terminal-Verwaltung (Zero-Touch Provisioning)</CardTitle>
              <CardDescription>
                Verwalten Sie Ihre Kiosk-Geräte. Fügen Sie neue Geräte über einen Pairing-Code hinzu.
              </CardDescription>
            </div>
            <Button onClick={handleGeneratePairingCode} disabled={isPairingLoading} className="gap-2">
              <MonitorSmartphone className="w-4 h-4" />
              Neues Terminal koppeln
            </Button>
          </CardHeader>
          <CardContent>
            {pairingCode && (
              <div className="bg-blue-50 border border-blue-200 rounded-lg p-6 mb-8 text-center animate-in fade-in zoom-in duration-300">
                <h3 className="text-lg font-semibold text-blue-900 mb-2">Ihr Pairing-Code</h3>
                <p className="text-sm text-blue-800 mb-4">Geben Sie diesen Code innerhalb von 15 Minuten am neuen Kiosk-Terminal ein.</p>
                <div className="text-4xl font-mono tracking-widest font-bold text-blue-600 bg-white inline-block py-3 px-8 rounded-lg shadow-sm border border-blue-100">
                  {pairingCode}
                </div>
              </div>
            )}

            <div className="rounded-md border border-slate-200">
              <Table>
                <TableHeader className="bg-slate-50">
                  <TableRow>
                    <TableHead>Gerätename</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Letzter Ping</TableHead>
                    <TableHead>Gekoppelt am</TableHead>
                    <TableHead className="text-right">Aktion</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {terminals.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={5} className="text-center py-8 text-slate-500">Keine Terminals gekoppelt</TableCell>
                    </TableRow>
                  ) : terminals.map((t) => (
                    <TableRow key={t.id}>
                      <TableCell className="font-medium flex items-center gap-2">
                        <MonitorSmartphone className="w-4 h-4 text-slate-500" />
                        {t.name}
                      </TableCell>
                      <TableCell>
                        {t.status === 'active' ? (
                          <span className="inline-flex items-center gap-1.5 py-1 px-2.5 rounded-full text-xs font-medium bg-green-50 text-green-700">
                            <span className="w-1.5 h-1.5 rounded-full bg-green-500"></span>
                            Aktiv
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1.5 py-1 px-2.5 rounded-full text-xs font-medium bg-red-50 text-red-700">
                            <span className="w-1.5 h-1.5 rounded-full bg-red-500"></span>
                            Gesperrt
                          </span>
                        )}
                      </TableCell>
                      <TableCell className="text-sm text-slate-600">
                        {t.last_ping ? format(new Date(t.last_ping), "dd.MM.yyyy HH:mm") : "-"}
                      </TableCell>
                      <TableCell className="text-sm text-slate-600">
                        {format(new Date(t.created_at), "dd.MM.yyyy")}
                      </TableCell>
                      <TableCell className="text-right">
                        <Button 
                          variant="outline" 
                          size="sm" 
                          className="text-red-600 hover:text-red-700 hover:bg-red-50"
                          onClick={() => handleRevokeTerminal(t.id)}
                          disabled={t.status === 'revoked'}
                        >
                          <Link2Off className="w-4 h-4 mr-1" />
                          Sperren
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>
      </TabsContent>

      {/* Video Call Dialog */}
      <Dialog
        open={!!activeVideoCall}
        onOpenChange={(open) => {
          if (!open) setActiveVideoCall(null);
        }}
      >
        <DialogContent className="sm:max-w-4xl h-[80vh] flex flex-col p-0 overflow-hidden">
          <DialogHeader className="p-4 pb-0">
            <DialogTitle>Live Videosprechstunde</DialogTitle>
          </DialogHeader>
          <div className="flex-1 w-full relative bg-black mt-2">
            {activeVideoCall && (
              <>
                {isReconnecting && (
                  <div className="absolute inset-0 z-40 flex items-center justify-center bg-black/80 text-white backdrop-blur-sm">
                    <div className="text-center">
                      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-white mx-auto mb-4"></div>
                      <p className="text-lg">Verbindung unterbrochen – warte auf Reconnect...</p>
                    </div>
                  </div>
                )}
                <JitsiMeeting
                  domain={jitsiDomain}
                  roomName={`atm-service-apotheke-${activeVideoCall}`}
                  configOverwrite={{
                    startWithAudioMuted: false,
                    startWithVideoMuted: false,
                    prejoinPageEnabled: false,
                    disableModeratorIndicator: true,
                    enableE2EE: true,
                    resolution: 480,
                    constraints: {
                      video: {
                        height: { ideal: 480, max: 480, min: 240 },
                        aspectRatio: 16 / 9,
                        frameRate: { ideal: 24, max: 30 }
                      }
                    },
                    p2p: { enabled: false },
                    disableAudioLevels: false
                  }}
                  interfaceConfigOverwrite={{
                    DISABLE_JOIN_LEAVE_NOTIFICATIONS: true,
                    SHOW_CHROME_EXTENSION_BANNER: false,
                  }}
                  userInfo={{
                    displayName: 'Apotheke'
                  }}
                  onApiReady={(externalApi) => {
                    externalApi.addListener('videoConferenceJoined', () => setIsReconnecting(false));
                    externalApi.addListener('participantLeft', () => setIsReconnecting(false));
                  }}
                  getIFrameRef={(iframeRef) => {
                    iframeRef.style.height = '100%';
                    iframeRef.style.width = '100%';
                    iframeRef.style.border = '0';
                  }}
                />
              </>
            )}
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
