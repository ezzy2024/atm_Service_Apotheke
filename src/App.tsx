/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import AdminLayout from "./pages/admin/AdminLayout";
import Dashboard from "./pages/admin/Dashboard";
import Settings from "./pages/admin/Settings";
import SuperAdminDashboard from "./pages/admin/SuperAdminDashboard";
import KioskLayout from "./pages/kiosk/KioskLayout";
import Session from "./pages/kiosk/Session";
import Standby from "./pages/kiosk/Standby";
import Login from "./pages/auth/Login";
import Register from "./pages/auth/Register";

import CookieBanner from "./components/CookieBanner";

export default function App() {
  return (
    <>
      <CookieBanner />
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Navigate to="/login" replace />} />
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

          {/* Kiosk Domain */}
          <Route path="/kiosk" element={<KioskLayout />}>
            <Route index element={<Standby />} />
            <Route path="session/:id" element={<Session />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </>
  );
}
