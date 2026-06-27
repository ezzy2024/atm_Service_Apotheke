import { Outlet } from 'react-router-dom';

export default function KioskLayout() {
  return (
    <div className="h-screen w-screen bg-slate-50 flex flex-col text-slate-900 overflow-hidden">
      <header className="h-20 bg-[#0082C8] flex items-center px-8 shadow-md shrink-0">
        <h1 className="text-2xl font-bold text-white tracking-tight">Service Apotheke aTM</h1>
      </header>
      
      <main className="flex-1 overflow-auto p-8 flex items-center justify-center">
        <div className="max-w-4xl w-full h-full bg-white rounded-2xl shadow-lg border border-slate-200 flex flex-col overflow-hidden relative">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
