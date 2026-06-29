import { useState, useEffect } from "react";
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
} from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { CheckCircle2, XCircle, Clock, FileText, ExternalLink } from "lucide-react";

export default function SuperAdminDashboard() {
  const [pharmacies, setPharmacies] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchPharmacies();
  }, []);

  const fetchPharmacies = async () => {
    try {
      const res = await fetch("/api/admin/pharmacies");
      if (res.ok) {
        const data = await res.json();
        setPharmacies(data);
      }
    } catch (e) {
      console.error("Error fetching pharmacies:", e);
    } finally {
      setLoading(false);
    }
  };

  const handleFirstApprove = async (id: string) => {
    try {
      const res = await fetch(`/api/admin/pharmacies/${id}`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ onboarding_status: "pending_documents" }),
      });
      if (res.ok) {
        alert("Erstfreigabe erteilt. Die Apotheke kann nun Dokumente hochladen.");
        fetchPharmacies();
      }
    } catch (e) {
      console.error(e);
    }
  };

  const handleVerifyDocuments = async (id: string, approve: boolean) => {
    try {
      const res = await fetch(`/api/admin/pharmacies/${id}/verify-documents`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ onboarding_status: approve ? "active" : "pending_documents" }),
      });
      if (res.ok) {
        alert(approve ? "Apotheke erfolgreich aktiviert und freigeschaltet!" : "Unterlagen abgelehnt.");
        fetchPharmacies();
      }
    } catch (e) {
      console.error(e);
    }
  };

  const handleBlock = async (id: string) => {
    try {
      const res = await fetch(`/api/admin/pharmacies/${id}/verify-documents`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ onboarding_status: "pending_approval" }),
      });
      if (res.ok) {
        alert("Apotheke gesperrt.");
        fetchPharmacies();
      }
    } catch (e) {
      console.error(e);
    }
  };

  const handleViewDoc = async (path: string) => {
    try {
      const res = await fetch(`/api/admin/pharmacy-document-url?report_path=${encodeURIComponent(path)}`);
      if (res.ok) {
        const data = await res.json();
        if (data.url) {
          window.open(data.url, "_blank");
        } else {
          alert("URL konnte nicht generiert werden.");
        }
      }
    } catch (e) {
      console.error(e);
      alert("Fehler beim Laden des Dokuments.");
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-slate-500 font-medium">Lade registrierte Apotheken...</div>
      </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto space-y-8 animate-in fade-in duration-500 font-sans">
      <div>
        <h1 className="text-3xl font-bold text-slate-900 tracking-tight">
          Super-Admin Dashboard
        </h1>
        <p className="text-slate-500 mt-2 text-lg">
          Prüfen und verwalten Sie B2B-Konto-Registrierungen, Verträge und Lizenzen.
        </p>
      </div>

      <Card className="border-slate-200 shadow-sm">
        <CardHeader>
          <CardTitle>Apotheken-Registrierungen</CardTitle>
          <CardDescription>
            Onboarding-Status und hochgeladene Nachweise der B2B-Kunden.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="rounded-md border border-slate-200 overflow-hidden">
            <Table>
              <TableHeader className="bg-slate-50">
                <TableRow>
                  <TableHead>Apotheke</TableHead>
                  <TableHead>IK-Nummer / BSNR</TableHead>
                  <TableHead>E-Mail (Ansprechpartner)</TableHead>
                  <TableHead>Onboarding-Status</TableHead>
                  <TableHead>Hochgeladene Dokumente</TableHead>
                  <TableHead className="text-right">Aktion</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {pharmacies.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} className="text-center py-8 text-slate-500">
                      Keine registrierten Apotheken in der Datenbank gefunden.
                    </TableCell>
                  </TableRow>
                ) : (
                  pharmacies.map((pharmacy) => (
                    <TableRow key={pharmacy.id}>
                      <TableCell className="font-semibold text-slate-800">
                        {pharmacy.name}
                      </TableCell>
                      <TableCell className="font-mono text-sm text-slate-600">
                        IK: {pharmacy.ik_nummer} <br />
                        BSNR: {pharmacy.bsnr}
                      </TableCell>
                      <TableCell className="text-slate-600 text-sm">
                        {pharmacy.ansprechpartner || "-"}
                      </TableCell>
                      <TableCell>
                        {pharmacy.onboarding_status === "pending_approval" && (
                          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800 gap-1">
                            <Clock className="w-3 h-3" />
                            Erstprüfung ausstehend
                          </span>
                        )}
                        {pharmacy.onboarding_status === "pending_documents" && (
                          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800 gap-1">
                            <FileText className="w-3 h-3" />
                            Unterlagen ausstehend
                          </span>
                        )}
                        {pharmacy.onboarding_status === "pending_verification" && (
                          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-indigo-100 text-indigo-800 gap-1">
                            <Clock className="w-3 h-3 animate-pulse" />
                            Dokumente in Prüfung
                          </span>
                        )}
                        {pharmacy.onboarding_status === "active" && (
                          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800 gap-1">
                            <CheckCircle2 className="w-3 h-3" />
                            Aktiviert
                          </span>
                        )}
                      </TableCell>
                      <TableCell className="space-y-1">
                        {pharmacy.operating_license_path && (
                          <button
                            onClick={() => handleViewDoc(pharmacy.operating_license_path)}
                            className="flex items-center gap-1 text-[#0082C8] hover:text-[#006A9C] text-xs font-semibold cursor-pointer"
                          >
                            <FileText className="w-3.5 h-3.5" />
                            Betriebserlaubnis <ExternalLink className="w-3 h-3" />
                          </button>
                        )}
                        {pharmacy.approbationsurkunde_path && (
                          <button
                            onClick={() => handleViewDoc(pharmacy.approbationsurkunde_path)}
                            className="flex items-center gap-1 text-[#0082C8] hover:text-[#006A9C] text-xs font-semibold cursor-pointer"
                          >
                            <FileText className="w-3.5 h-3.5" />
                            Approbation <ExternalLink className="w-3 h-3" />
                          </button>
                        )}
                        {pharmacy.avv_document_path && (
                          <button
                            onClick={() => handleViewDoc(pharmacy.avv_document_path)}
                            className="flex items-center gap-1 text-[#0082C8] hover:text-[#006A9C] text-xs font-semibold cursor-pointer"
                          >
                            <FileText className="w-3.5 h-3.5" />
                            AVV-Vertrag <ExternalLink className="w-3 h-3" />
                          </button>
                        )}
                        {!pharmacy.operating_license_path && !pharmacy.approbationsurkunde_path && !pharmacy.avv_document_path && (
                          <span className="text-slate-400 text-xs">-</span>
                        )}
                      </TableCell>
                      <TableCell className="text-right space-x-2">
                        {pharmacy.onboarding_status === "pending_approval" && (
                          <Button
                            size="sm"
                            onClick={() => handleFirstApprove(pharmacy.id)}
                            className="bg-[#0082C8] hover:bg-[#006A9C] text-white text-xs font-bold py-1 px-3 cursor-pointer"
                          >
                            Erstfreigabe erteilen
                          </Button>
                        )}
                        {pharmacy.onboarding_status === "pending_verification" && (
                          <>
                            <Button
                              size="sm"
                              onClick={() => handleVerifyDocuments(pharmacy.id, true)}
                              className="bg-green-600 hover:bg-green-700 text-white text-xs font-bold py-1 px-3 cursor-pointer"
                            >
                              Aktivieren
                            </Button>
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => handleVerifyDocuments(pharmacy.id, false)}
                              className="border-red-200 text-red-700 hover:bg-red-50 text-xs font-bold py-1 px-3 cursor-pointer"
                            >
                              Ablehnen
                            </Button>
                          </>
                        )}
                        {pharmacy.onboarding_status === "active" && (
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => handleBlock(pharmacy.id)}
                            className="border-amber-300 text-amber-800 hover:bg-amber-50 text-xs font-bold py-1 px-3 cursor-pointer"
                          >
                            Sperren
                          </Button>
                        )}
                        {pharmacy.onboarding_status === "pending_documents" && (
                          <span className="text-slate-400 text-xs font-medium">Warte auf Upload...</span>
                        )}
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
