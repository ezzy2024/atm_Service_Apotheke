import { useNavigate } from "react-router-dom";
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  CardFooter,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Stethoscope } from "lucide-react";

export default function Login() {
  const navigate = useNavigate();

  const handleDemoLogin = (
    role: "pharmacist" | "pharmacy_admin" | "super_admin",
  ) => {
    localStorage.setItem("demo_pharmacy_id", "demo-ik-123456789");
    localStorage.setItem("demo_auth_token", "mock-jwt-token");
    localStorage.setItem("demo_role", role);
    localStorage.setItem("demo_pharmacist_name", "Dr. Max Mustermann");

    if (role === "super_admin") {
      navigate("/super-admin");
    } else if (role === "pharmacy_admin") {
      navigate("/admin/settings");
    } else {
      navigate("/admin/dashboard");
    }
  };

  return (
    <div className="min-h-screen bg-slate-50 flex items-center justify-center p-4">
      <Card className="w-full max-w-md shadow-lg border-slate-200">
        <CardHeader className="text-center space-y-2">
          <div className="w-16 h-16 bg-[#0082C8]/10 rounded-full flex items-center justify-center mx-auto mb-4">
            <Stethoscope className="w-8 h-8 text-[#0082C8]" />
          </div>
          <CardTitle className="text-2xl font-bold text-slate-900 tracking-tight">
            Service Apotheke aTM
          </CardTitle>
          <CardDescription className="text-slate-500">
            Loggen Sie sich ein, um auf das Apotheken-Cockpit zuzugreifen.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <Button
            className="w-full bg-[#0082C8] hover:bg-[#006A9C] text-white py-6 text-lg"
            disabled
          >
            Mit Supabase Anmelden
          </Button>
          <div className="relative">
            <div className="absolute inset-0 flex items-center">
              <span className="w-full border-t border-slate-200" />
            </div>
            <div className="relative flex justify-center text-xs uppercase">
              <span className="bg-white px-2 text-slate-500">
                Oder zum Testen (Demo)
              </span>
            </div>
          </div>
          <div className="space-y-2">
            <Button
              variant="outline"
              className="w-full py-4 text-sm border-slate-300 text-slate-700 hover:bg-slate-50"
              onClick={() => handleDemoLogin("pharmacist")}
            >
              Als Apotheker (Dashboard)
            </Button>
            <Button
              variant="outline"
              className="w-full py-4 text-sm border-slate-300 text-slate-700 hover:bg-slate-50"
              onClick={() => handleDemoLogin("pharmacy_admin")}
            >
              Als Apotheken-Admin (AVV)
            </Button>
            <Button
              variant="outline"
              className="w-full py-4 text-sm border-slate-300 text-slate-700 hover:bg-slate-50"
              onClick={() => handleDemoLogin("super_admin")}
            >
              Als Super-Admin (Freigaben)
            </Button>
          </div>
        </CardContent>
        <CardFooter className="flex-col gap-4 text-center text-sm text-slate-500 justify-center">
          Für Demonstrationszwecke der B2B SaaS Anwendung.
          <Button
            variant="link"
            onClick={() => navigate("/register")}
            className="text-[#0082C8] p-0 h-auto"
          >
            Apotheke registrieren (B2B)
          </Button>
        </CardFooter>
      </Card>
    </div>
  );
}
