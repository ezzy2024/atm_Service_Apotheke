import { useState, useEffect } from "react";
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
  const [activeVideoCall, setActiveVideoCall] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isAddEventOpen, setIsAddEventOpen] = useState(false);
  const [newEvent, setNewEvent] = useState({
    title: "",
    start: new Date(),
    end: addMinutes(new Date(), 30),
  });

  useEffect(() => {
    fetchData();

    // Listen for Kiosk alerts (cross-tab via BroadcastChannel - fallback)
    const channel = new BroadcastChannel("kiosk_alerts");
    channel.onmessage = (event) => {
      if (event.data.type === "triage_completed") {
        alert(
          `Neue Patienten-Meldung aus dem Kiosk!\nLeistungsart: ${event.data.serviceType === "triage_only" ? "Nur Ersteinschätzung" : "Ersteinschätzung + Video"}`,
        );
        fetchData();
      }
    };

    // Polling fallback to check for database updates every 10 seconds
    const interval = setInterval(() => {
      fetchData();
    }, 10000);

    return () => {
      channel.close();
      clearInterval(interval);
    };
  }, []);

  const fetchData = async () => {
    setIsLoading(true);
    const pharmacyId = localStorage.getItem("demo_pharmacy_id") || "d3b07384-d113-4956-a50e-a1c563e4410a";
    try {
      // 1. Consent Agreements
      const consentRes = await fetch(`/api/admin/consents?pharmacy_id=${pharmacyId}`);
      if (consentRes.ok) {
        const consentData = await consentRes.json();
        if (consentData && consentData.length > 0) {
          setAgreements(consentData);
        }
      }

      // 2. Billing Records
      const billingRes = await fetch(`/api/admin/billing?pharmacy_id=${pharmacyId}`);
      if (billingRes.ok) {
        const billingData = await billingRes.json();
        if (billingData && billingData.length > 0) {
          setBillingRecords(billingData);
        }
      }

      // 3. Appointments
      const aptRes = await fetch(`/api/admin/appointments?pharmacy_id=${pharmacyId}`);
      if (aptRes.ok) {
        const aptData = await aptRes.json();
        if (aptData && aptData.length > 0) {
          setAppointments(aptData);
        }
      }
    } catch (e) {
      console.log("Using mock data due to fetch error:", e);
    }
    setIsLoading(false);
  };

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

  const handleJoinVideo = (recordId: string) => {
    setActiveVideoCall(recordId);
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

  const generateBillingExport = () => {
    let csvContent = "data:text/csv;charset=utf-8,";
    csvContent +=
      "Apotheken_IK,BSNR,Krankenkasse,KVNR,Status,Sonderkennzeichen,Datum,Betrag,Ausfuehrender_Apotheker\n";

    const recordsToExport =
      billingRecords.length > 0
        ? billingRecords
        : agreements.map((c) => ({
            consent_agreements: c,
            service_type: "triage_only",
            date_of_service: c.signed_date,
            sonderkennzeichen: getSonderkennzeichen("triage_only"),
            amount: calculateAmount(c.signed_date),
            executed_by_pharmacist_name:
              localStorage.getItem("demo_pharmacist_name") || "Apotheker",
          }));

    recordsToExport.forEach((record: any) => {
      const consent = record.consent_agreements;
      const dateStr = record.date_of_service
        ? format(new Date(record.date_of_service), "dd.MM.yyyy")
        : "";

      const ikApo = "123456789"; // Dummy Apotheke IK
      const bsnr = "000000000";
      const ikKk = consent?.ik_number || "";
      const kvnr = consent?.health_insurance_number || "";
      const status = consent?.status_field || "0000083";
      const sk =
        record.sonderkennzeichen || getSonderkennzeichen(record.service_type);
      const amount = record.amount || calculateAmount(record.date_of_service);
      const pharmacist = record.executed_by_pharmacist_name || "";

      csvContent += `${ikApo},${bsnr},${ikKk},${kvnr},${status},${sk},${dateStr},${amount.toFixed(2)},${pharmacist}\n`;
    });

    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute(
      "download",
      `TA3_Sonderbeleg_${format(new Date(), "yyyyMMdd")}.csv`,
    );
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
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
      patient_name: newEvent.title,
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
          patient_name: newEvent.title,
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

  return (
    <div className="max-w-6xl mx-auto space-y-8 animate-in fade-in duration-500">
      <div>
        <h1 className="text-3xl font-bold text-slate-900 tracking-tight">
          Übersicht
        </h1>
        <p className="text-slate-500 mt-2">
          Verwalten Sie hier Termine, Einverständniserklärungen und Abrechnungen
          für die assistierte Telemedizin.
        </p>
      </div>

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
            <CardHeader className="flex flex-row items-start justify-between">
              <div>
                <CardTitle>Abrechnungs-Generator</CardTitle>
                <CardDescription>
                  Erzeugen Sie Sonderbeleg-Daten für erbrachte aTM-Leistungen.
                </CardDescription>
              </div>
              <Button
                onClick={generateBillingExport}
                className="bg-[#0082C8] hover:bg-[#006A9C] text-white gap-2"
              >
                <Download className="w-4 h-4" />
                Sonderbeleg Exportieren
              </Button>
            </CardHeader>
            <CardContent>
              <div className="rounded-md border border-slate-200">
                <Table>
                  <TableHeader className="bg-slate-50">
                    <TableRow>
                      <TableHead>Datum</TableHead>
                      <TableHead>Leistungsart</TableHead>
                      <TableHead>Sonderkennzeichen</TableHead>
                      <TableHead className="text-right">Betrag</TableHead>
                      <TableHead className="text-right">Aktionen</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {(billingRecords.length > 0
                      ? billingRecords
                      : agreements.map((c, idx) => ({
                          id: c.id,
                          date_of_service: c.signed_date,
                          service_type: [
                            "triage_only",
                            "video_only",
                            "triage_and_video",
                          ][idx % 3] as ServiceType,
                          sonderkennzeichen: getSonderkennzeichen(
                            ["triage_only", "video_only", "triage_and_video"][
                              idx % 3
                            ] as ServiceType,
                          ),
                          amount: calculateAmount(c.signed_date),
                          report_path: undefined,
                        }))
                    ).map((record) => {
                      return (
                        <TableRow key={record.id}>
                          <TableCell>
                            {format(
                              new Date(record.date_of_service),
                              "dd.MM.yyyy",
                            )}
                          </TableCell>
                          <TableCell>
                            {record.service_type === "triage_only" &&
                              "Nur Ersteinschätzung"}
                            {record.service_type === "video_only" &&
                              "Nur Videosprechstunde"}
                            {record.service_type === "triage_and_video" &&
                              "Ersteinschätzung + Video"}
                          </TableCell>
                          <TableCell className="font-mono text-sm">
                            {record.sonderkennzeichen ||
                              getSonderkennzeichen(record.service_type)}
                          </TableCell>
                          <TableCell className="text-right font-medium">
                            {(
                              record.amount ||
                              calculateAmount(record.date_of_service)
                            ).toFixed(2)}{" "}
                            €
                          </TableCell>
                          <TableCell className="text-right space-x-2">
                            {(record.service_type === "video_only" || record.service_type === "triage_and_video") && (
                              <Button
                                size="sm"
                                className="bg-[#0082C8] hover:bg-[#006A9C] text-white text-xs font-bold py-1 px-3 cursor-pointer"
                                onClick={() => handleJoinVideo(record.id)}
                              >
                                Videosprechstunde beitreten
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
                      );
                    })}
                  </TableBody>
                </Table>
              </div>
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
              <Label>Titel</Label>
              <Input
                value={newEvent.title}
                onChange={(e) =>
                  setNewEvent({ ...newEvent, title: e.target.value })
                }
                placeholder="z.B. Wartung oder Termin"
              />
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
              <JitsiMeeting
                roomName={`atm-service-apotheke-${activeVideoCall}`}
                configOverwrite={{
                  startWithAudioMuted: false,
                  startWithVideoMuted: false,
                  disableModeratorIndicator: true,
                  enableEmailInStats: false,
                  prejoinPageEnabled: false,
                }}
                interfaceConfigOverwrite={{
                  DISABLE_JOIN_LEAVE_NOTIFICATIONS: true,
                  SHOW_CHROME_EXTENSION_BANNER: false,
                }}
                userInfo={{
                  displayName: "Apotheker",
                  email: "apotheke@serviceapotheke.tech"
                }}
                getIFrameRef={(iframeRef) => {
                  if (iframeRef) {
                    iframeRef.style.height = "100%";
                    iframeRef.style.width = "100%";
                  }
                }}
              />
            )}
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
