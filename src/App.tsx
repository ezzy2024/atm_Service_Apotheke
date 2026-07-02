/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { Toaster } from "sonner";
import AdminLayout from "./pages/admin/AdminLayout";
import Dashboard from "./pages/admin/Dashboard";
import Settings from "./pages/admin/Settings";
import SuperAdminDashboard from "./pages/admin/SuperAdminDashboard";
import PatientLayout from "./pages/patient/PatientLayout";
import Session from "./pages/patient/Session";
import Standby from "./pages/patient/Standby";
import Login from "./pages/auth/Login";
import Register from "./pages/auth/Register";
import Landing from "./pages/Landing";
import Security from "./pages/Security";

import CookieBanner from "./components/CookieBanner";

export default function App() {
  return (
    <>
      <Toaster position="top-center" richColors />
      <CookieBanner />
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Landing />} />
          <Route path="/security" element={<Security />} />
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />

          {/* Admin Domain */}
          <Route path="/admin" element={<AdminLayout />}>
            <Route path="dashboard" element={<Dashboard />} />
            <Route path="settings" element={<Settings />} />
          </Route>

          {/* Super Admin Domain */}
          <Route path="/super-admin" element={<AdminLayout />}>
            <Route index element={<SuperAdminDashboard />} />
          </Route>

          {/* Patient BYOD Domain */}
          <Route path="/patient" element={<PatientLayout />}>
            <Route index element={<Standby />} />
            <Route path="session/:id" element={<Session />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </>
  );
}
