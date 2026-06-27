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
import { CheckCircle2, XCircle, Clock } from "lucide-react";
import { supabase } from "@/lib/supabase";
import { Pharmacy } from "@/src/types";

// Mock data
const MOCK_PHARMACIES: Pharmacy[] = [
  {
    id: "1",
    name: "Stadt-Apotheke am Markt",
    ik_nummer: "123456789",
    bsnr: "000000000",
    is_approved: false,
    created_at: new Date().toISOString(),
  },
  {
    id: "2",
    name: "Rathaus Apotheke",
    ik_nummer: "987654321",
    bsnr: "111111111",
    is_approved: true,
    created_at: new Date(Date.now() - 86400000).toISOString(),
  },
];

export default function SuperAdminDashboard() {
  const [pharmacies, setPharmacies] = useState<Pharmacy[]>(MOCK_PHARMACIES);

  useEffect(() => {
    fetchPharmacies();
  }, []);

  const fetchPharmacies = async () => {
    try {
      const { data, error } = await supabase
        .from("pharmacies")
        .select("*")
        .order("created_at", { ascending: false });
      if (data && data.length > 0) {
        setPharmacies(data);
      }
    } catch (e) {
      console.log("Using mock pharmacies");
    }
  };

  const handleApprove = async (id: string, currentStatus: boolean) => {
    // In demo mode we just update state
    setPharmacies(
      pharmacies.map((p) =>
        p.id === id ? { ...p, is_approved: !currentStatus } : p,
      ),
    );

    // Attempt real DB update
    try {
      await supabase
        .from("pharmacies")
        .update({ is_approved: !currentStatus })
        .eq("id", id);
    } catch (e) {
      // Ignore
    }
  };

  return (
    <div className="max-w-6xl mx-auto space-y-8 animate-in fade-in duration-500">
      <div>
        <h1 className="text-3xl font-bold text-slate-900 tracking-tight">
          Super-Admin Dashboard
        </h1>
        <p className="text-slate-500 mt-2 text-lg">
          Verwalten Sie Apotheken-Registrierungen und Systemfreigaben.
        </p>
      </div>

      <Card className="border-slate-200 shadow-sm">
        <CardHeader>
          <CardTitle>Registrierte Apotheken</CardTitle>
          <CardDescription>
            Prüfen und genehmigen Sie neue B2B-Kunden.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="rounded-md border border-slate-200">
            <Table>
              <TableHeader className="bg-slate-50">
                <TableRow>
                  <TableHead>Apotheke</TableHead>
                  <TableHead>IK-Nummer</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Aktion</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {pharmacies.map((pharmacy) => (
                  <TableRow key={pharmacy.id}>
                    <TableCell className="font-medium">
                      {pharmacy.name}
                    </TableCell>
                    <TableCell className="font-mono text-sm">
                      {pharmacy.ik_nummer}
                    </TableCell>
                    <TableCell>
                      {pharmacy.is_approved ? (
                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800 gap-1">
                          <CheckCircle2 className="w-3 h-3" />
                          Freigegeben
                        </span>
                      ) : (
                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800 gap-1">
                          <Clock className="w-3 h-3" />
                          Ausstehend
                        </span>
                      )}
                    </TableCell>
                    <TableCell className="text-right">
                      <Button
                        variant={pharmacy.is_approved ? "outline" : "default"}
                        size="sm"
                        onClick={() =>
                          handleApprove(
                            pharmacy.id,
                            pharmacy.is_approved || false,
                          )
                        }
                        className={
                          !pharmacy.is_approved
                            ? "bg-[#0082C8] hover:bg-[#006A9C] text-white"
                            : ""
                        }
                      >
                        {pharmacy.is_approved ? "Sperren" : "Freigeben"}
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
