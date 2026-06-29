import { useState, useEffect } from "react";
import { format, addMinutes } from "date-fns";
import { de } from "date-fns/locale";
import {
  Calendar as CalendarIcon,
  FileSignature,
  Receipt,
  Download,
  FileText,
  CheckCircle2,
  Plus,
  Video,
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
  const [appointments, setAppointments] =
    useState<Appointment[]>(MOCK_APPOINTMENTS);
  const [consents, setConsents] = useState<ConsentAgreement[]>(MOCK_CONSENTS);
  const [billingRecords, setBillingRecords] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isAddEventOpen, setIsAddEventOpen] = useState(false);
  const [activeVideoConsentId, setActiveVideoConsentId] = useState<string | null>(null);
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

    // Listen for Kiosk alerts (Realtime via Supabase)
    const pharmacyId = localStorage.getItem("demo_pharmacy_id");
    const subscription = supabase
      .channel("schema-db-changes")
      .on(
        "postgres_changes",
        {
          event: "*",
          schema: "public",
          table: "billing_records",
          filter: pharmacyId ? `pharmacy_id=eq.${pharmacyId}` : undefined,
        },
        (payload) => {
          console.log("Realtime DB Event:", payload);
          if (
            payload.eventType === "INSERT" ||
            payload.eventType === "UPDATE"
          ) {
            alert("Patient wartet im aTM-Raum (Datenbank-Benachrichtigung)");
            fetchData();
          }
        },
      )
      .subscribe();

    return () => {
      channel.close();
      supabase.removeChannel(subscription);
    };
  }, []);

  const fetchData = async () => {
    setIsLoading(true);
    try {
      const { data: consentData, error: consentErr } = await supabase
        .from("consent_agreements")
        .select("*")
        .order("signed_date", { ascending: false });
      if (!consentErr && consentData && consentData.length > 0) {
        setConsents(consentData);
      }

      const { data: billingData, error: billingErr } = await supabase
        .from("billing_records")
        .select(
          `
          *,
          consent_agreements (
            patient_name,
            health_insurance_name,
            health_insurance_number,
            ik_number,
            birth_date,
            status_field
          )
        `,
        )
        .order("date_of_service", { ascending: false });

      if (!billingErr && billingData && billingData.length > 0) {
        setBillingRecords(billingData);
      }

      const { data: aptData, error: aptErr } = await supabase
        .from("appointments")
        .select("*");
      if (!aptErr && aptData && aptData.length > 0) {
        setAppointments(aptData);
      }
    } catch (e) {
      console.log("Using mock data");
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

  const generateBillingExport = () => {
    let csvContent = "data:text/csv;charset=utf-8,";
    csvContent +=
      "Apotheken_IK,BSNR,Krankenkasse,KVNR,Status,Sonderkennzeichen,Datum,Betrag,Ausfuehrender_Apotheker\n";

    const recordsToExport =
      billingRecords.length > 0
        ? billingRecords
        : consents.map((c) => ({
            consent_agreements: c,
            service_type: "triage_only",
            date_of_service: c.signed_date,
            sonderkennzeichen: getSonderkennzeichen("triage_only"),
            amount: calculateAmount(c.signed_date),
            executed_by_pharmacist_name:
              localStorage.getItem("demo_pharmacist_name") || "Apotheker",
          }));

    recordsToExport.forEach((record) => {
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
      await supabase.from("appointments").insert([newApt]);
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
            <DialogContent className="sm:max-w-[425px]">
              <DialogHeader>
                <DialogTitle>Neuen Termin eintragen</DialogTitle>
              </DialogHeader>
              <div className="grid gap-4 py-4">
                <div className="grid grid-cols-4 items-center gap-4">
                  <Label htmlFor="patient" className="text-right">
                    Patient
                  </Label>
                  <Input
                    id="patient"
                    value={newEvent.title}
                    onChange={(e) =>
                      setNewEvent({ ...newEvent, title: e.target.value })
                    }
                    className="col-span-3"
                    placeholder="Name des Patienten"
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
                    {consents.map((c) => {
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
                      <TableHead className="text-right">Aktion</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {(billingRecords.length > 0
                      ? billingRecords
                      : consents.map((c, idx) => ({
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
                          <TableCell className="text-right">
                            {(record.service_type === "video_only" ||
                              record.service_type === "triage_and_video") && (
                              <Button
                                size="sm"
                                className="bg-[#0082C8] hover:bg-[#006A9C] text-white gap-2"
                                onClick={() => setActiveVideoConsentId(record.consent_id || record.id)}
                              >
                                <Video className="w-4 h-4" />
                                Beitreten
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

      {/* Video Call Modal */}
      <Dialog open={!!activeVideoConsentId} onOpenChange={(open) => !open && setActiveVideoConsentId(null)}>
        <DialogContent className="max-w-[90vw] w-[1200px] h-[80vh] p-0 overflow-hidden bg-slate-900 border-none">
          {activeVideoConsentId && (
            <JitsiMeeting
              domain="meet.jit.si"
              roomName={`ServiceApotheke-aTM-${activeVideoConsentId}`}
              configOverwrite={{
                startWithAudioMuted: false,
                startWithVideoMuted: false,
                prejoinPageEnabled: false,
                disableDeepLinking: true,
              }}
              userInfo={{
                displayName: localStorage.getItem("demo_pharmacist_name") || "Apotheken-Personal",
                email: "",
              }}
              getIFrameRef={(iframeRef) => {
                iframeRef.style.height = "100%";
                iframeRef.style.width = "100%";
              }}
              onApiReady={(externalApi) => {
                externalApi.addListener("videoConferenceLeft", () => {
                  setActiveVideoConsentId(null);
                });
              }}
            />
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
