import { Outlet, NavLink, useNavigate, useLocation } from "react-router-dom";
import {
  LayoutDashboard,
  Users,
  FileText,
  Monitor,
  LogOut,
  Settings as SettingsIcon,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import Chatbot from "@/components/Chatbot";

export default function AdminLayout() {
  const navigate = useNavigate();
  const location = useLocation();
  const role = localStorage.getItem("demo_role") || "pharmacist";
  const name = localStorage.getItem("demo_pharmacist_name") || "Apotheker";

  const handleLogout = () => {
    localStorage.removeItem("demo_pharmacy_id");
    localStorage.removeItem("demo_auth_token");
    localStorage.removeItem("demo_role");
    localStorage.removeItem("demo_pharmacist_name");
    navigate("/login");
  };

  return (
    <div className="flex h-screen bg-slate-50 text-slate-900">
      <aside className="w-64 bg-white border-r border-slate-200 flex flex-col">
        <div className="p-6 border-b border-slate-200">
          <h1 className="text-xl font-bold text-[#0082C8] tracking-tight">
            Service Apotheke
          </h1>
          <p className="text-xs text-slate-500 mt-1 font-medium">
            aTM Management
          </p>
        </div>

        <nav className="flex-1 p-4 space-y-1">
          {(role === "pharmacist" || role === "pharmacy_admin") && (
            <NavLink
              to="/admin/dashboard"
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2.5 rounded-md text-sm font-medium transition-colors ${isActive ? "bg-[#0082C8]/10 text-[#0082C8]" : "text-slate-600 hover:bg-slate-100"}`
              }
            >
              <LayoutDashboard className="w-4 h-4" />
              Dashboard
            </NavLink>
          )}

          {role === "pharmacy_admin" && (
            <NavLink
              to="/admin/settings"
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2.5 rounded-md text-sm font-medium transition-colors ${isActive ? "bg-[#0082C8]/10 text-[#0082C8]" : "text-slate-600 hover:bg-slate-100"}`
              }
            >
              <SettingsIcon className="w-4 h-4" />
              Einstellungen (AVV)
            </NavLink>
          )}

          {role === "super_admin" && (
            <NavLink
              to="/super-admin"
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2.5 rounded-md text-sm font-medium transition-colors ${isActive ? "bg-[#0082C8]/10 text-[#0082C8]" : "text-slate-600 hover:bg-slate-100"}`
              }
            >
              <LayoutDashboard className="w-4 h-4" />
              Super-Admin Dashboard
            </NavLink>
          )}
        </nav>

        {(role === "pharmacist" || role === "pharmacy_admin") && (
          <div className="p-4 border-t border-slate-200">
            <Button
              variant="outline"
              className="w-full flex items-center justify-start gap-2 text-slate-600"
              onClick={() => window.open("/kiosk", "_blank")}
            >
              <Monitor className="w-4 h-4" />
              Kiosk-Modus starten
            </Button>
          </div>
        )}
      </aside>

      <main className="flex-1 flex flex-col min-w-0 overflow-hidden relative">
        <header className="h-16 bg-white border-b border-slate-200 flex items-center px-8 justify-between shrink-0">
          <h2 className="text-lg font-semibold text-slate-800">
            {role === "super_admin"
              ? "Super-Admin Dashboard"
              : "Apotheken-Cockpit"}
          </h2>
          <div className="flex items-center gap-4">
            <div className="text-sm text-slate-500">
              Angemeldet als {role === "super_admin" ? "Super-Admin" : name}
            </div>
            <Button variant="ghost" size="icon" onClick={handleLogout}>
              <LogOut className="w-4 h-4 text-slate-500" />
            </Button>
          </div>
        </header>

        <div className="flex-1 overflow-auto p-8 relative">
          <Outlet />
        </div>

        {/* Persistent Chatbot for Admin */}
        <Chatbot />
      </main>
    </div>
  );
}
