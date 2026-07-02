import { Link } from "react-router-dom";
import { ShieldCheck, Smartphone, Video, Lock, CheckCircle2, ArrowRight } from "lucide-react";
import { Button } from "@/components/ui/button";

export default function Landing() {
  return (
    <div className="min-h-screen bg-slate-50 font-sans">
      {/* Navigation */}
      <nav className="bg-white border-b border-slate-200 sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16 items-center">
            <div className="flex-shrink-0 flex items-center gap-2">
              <div className="w-8 h-8 bg-red-600 rounded flex items-center justify-center">
                <ShieldCheck className="w-5 h-5 text-white" />
              </div>
              <span className="font-bold text-xl text-slate-900 tracking-tight">aTM<span className="text-red-600">.</span>cloud</span>
            </div>
            <div className="hidden md:flex space-x-8">
              <a href="#features" className="text-slate-600 hover:text-red-600 font-medium transition-colors">Funktionen</a>
              <a href="#security" className="text-slate-600 hover:text-red-600 font-medium transition-colors">Sicherheit</a>
              <a href="#pricing" className="text-slate-600 hover:text-red-600 font-medium transition-colors">Preise</a>
            </div>
            <div className="flex items-center space-x-4">
              <Link to="/login" className="text-slate-600 hover:text-slate-900 font-medium">Kunden-Login</Link>
              <Link to="/patient">
                <Button variant="outline" className="hidden sm:inline-flex border-slate-300 text-slate-700 hover:bg-slate-50">
                  Zum Patienten-Portal
                </Button>
              </Link>
            </div>
          </div>
        </div>
      </nav>

      {/* Hero Section */}
      <section className="bg-white pt-20 pb-24 border-b border-slate-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl sm:text-5xl lg:text-6xl font-extrabold text-slate-900 tracking-tight mb-6">
            Cloud-native Telepharmazie.<br />
            <span className="text-red-600">Sicher, effizient, hardwarefrei.</span>
          </h1>
          <p className="text-lg sm:text-xl text-slate-500 max-w-2xl mx-auto mb-10 font-medium">
            Verbinden Sie Ihre Apotheke nahtlos mit Patienten via Bring-Your-Own-Device (BYOD). Keine teuren Kiosksysteme, 100% webbasiert und DSGVO-konform.
          </p>
          <div className="flex flex-col sm:flex-row justify-center gap-4">
            <Button className="bg-red-600 hover:bg-red-700 text-white h-14 px-8 text-lg rounded-lg shadow-lg font-semibold transition-all">
              Demo vereinbaren
            </Button>
            <Link to="/security">
              <Button variant="outline" className="h-14 px-8 text-lg border-slate-300 text-slate-700 hover:bg-slate-50 rounded-lg font-semibold w-full sm:w-auto">
                Architektur & Compliance ansehen
              </Button>
            </Link>
          </div>
        </div>
      </section>

      {/* Features Grid */}
      <section id="features" className="py-24 bg-slate-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-16">
            <h2 className="text-3xl font-bold text-slate-900 tracking-tight">Digitale Workflows für die moderne Apotheke</h2>
            <p className="mt-4 text-slate-500 text-lg">Optimieren Sie Ihre Prozesse ohne komplexe IT-Infrastruktur.</p>
          </div>
          
          <div className="grid md:grid-cols-3 gap-10">
            <div className="bg-white p-8 rounded-2xl shadow-sm border border-slate-200 hover:shadow-md transition-shadow">
              <div className="w-12 h-12 bg-red-50 rounded-lg flex items-center justify-center mb-6">
                <Smartphone className="w-6 h-6 text-red-600" />
              </div>
              <h3 className="text-xl font-bold text-slate-900 mb-3">BYOD Patienten-Flow</h3>
              <p className="text-slate-600 leading-relaxed">
                Patienten scannen einen QR-Code und durchlaufen die gesicherte Triage direkt auf dem eigenen Smartphone. Kein Hardware-Wartungsaufwand.
              </p>
            </div>
            
            <div className="bg-white p-8 rounded-2xl shadow-sm border border-slate-200 hover:shadow-md transition-shadow">
              <div className="w-12 h-12 bg-blue-50 rounded-lg flex items-center justify-center mb-6">
                <Video className="w-6 h-6 text-blue-600" />
              </div>
              <h3 className="text-xl font-bold text-slate-900 mb-3">Sichere Videokonsultation</h3>
              <p className="text-slate-600 leading-relaxed">
                Integrierte WebRTC-Videotelefonie. KBV-zertifizierter Datenschutz für die vertrauliche Beratung zwischen Apotheker und Patient.
              </p>
            </div>
            
            <div className="bg-white p-8 rounded-2xl shadow-sm border border-slate-200 hover:shadow-md transition-shadow">
              <div className="w-12 h-12 bg-emerald-50 rounded-lg flex items-center justify-center mb-6">
                <Lock className="w-6 h-6 text-emerald-600" />
              </div>
              <h3 className="text-xl font-bold text-slate-900 mb-3">PDF/A Audit-Protokolle</h3>
              <p className="text-slate-600 leading-relaxed">
                Automatische, manipulationssichere Generierung von Anamnese-Protokollen und Speicherung in dedizierten, verschlüsselten Buckets.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* Security / Trust Section */}
      <section id="security" className="py-24 bg-slate-900 text-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid lg:grid-cols-2 gap-16 items-center">
            <div>
              <h2 className="text-3xl font-bold tracking-tight mb-6">Kompromisslose Sicherheit & DSGVO-Konformität</h2>
              <p className="text-slate-300 text-lg mb-8 leading-relaxed">
                Unsere Plattform wurde nach dem Security-by-Design Prinzip entwickelt. Wir nutzen modernste Authentifizierungsverfahren und strikte Datenisolierung, um höchste medizinische Standards zu erfüllen.
              </p>
              <ul className="space-y-4">
                <li className="flex items-center gap-3">
                  <CheckCircle2 className="w-6 h-6 text-emerald-400" />
                  <span className="font-medium">Mandantenfähige Row-Level-Security (RLS)</span>
                </li>
                <li className="flex items-center gap-3">
                  <CheckCircle2 className="w-6 h-6 text-emerald-400" />
                  <span className="font-medium">Verschlüsselung at-rest und in-transit (TLS 1.3)</span>
                </li>
                <li className="flex items-center gap-3">
                  <CheckCircle2 className="w-6 h-6 text-emerald-400" />
                  <span className="font-medium">Hosting in ISO-27001 zertifizierten Rechenzentren (Deutschland)</span>
                </li>
              </ul>
              <div className="mt-10">
                <Link to="/security">
                  <Button variant="outline" className="border-slate-700 bg-slate-800 text-white hover:bg-slate-700 h-12 px-6">
                    Technisches Whitepaper lesen <ArrowRight className="w-4 h-4 ml-2" />
                  </Button>
                </Link>
              </div>
            </div>
            <div className="bg-slate-800 rounded-2xl p-8 border border-slate-700 shadow-2xl">
              <div className="flex items-center gap-4 mb-6 pb-6 border-b border-slate-700">
                <ShieldCheck className="w-12 h-12 text-emerald-400" />
                <div>
                  <h4 className="font-bold text-xl">Zertifizierte Infrastruktur</h4>
                  <p className="text-slate-400 text-sm">Bereit für den Einsatz in der Telematikinfrastruktur</p>
                </div>
              </div>
              <div className="space-y-4 text-sm text-slate-300 font-mono">
                <div className="flex justify-between"><span>JWT Validation</span><span className="text-emerald-400">ACTIVE</span></div>
                <div className="flex justify-between"><span>Audit Logging</span><span className="text-emerald-400">ENFORCED</span></div>
                <div className="flex justify-between"><span>Multi-Tenant DB</span><span className="text-emerald-400">ISOLATED</span></div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Pricing Section */}
      <section id="pricing" className="py-24 bg-slate-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-16">
            <h2 className="text-3xl font-bold text-slate-900 tracking-tight">Transparente Preismodelle</h2>
            <p className="mt-4 text-slate-500 text-lg">Skalierbar für einzelne Apotheken bis hin zu Apothekenverbünden.</p>
          </div>
          
          <div className="grid md:grid-cols-2 gap-8 max-w-4xl mx-auto">
            {/* Basic Tier */}
            <div className="bg-white rounded-3xl p-8 shadow-sm border border-slate-200">
              <h3 className="text-2xl font-bold text-slate-900 mb-2">Basic</h3>
              <p className="text-slate-500 mb-6">Für die einzelne Vor-Ort-Apotheke.</p>
              <div className="text-4xl font-extrabold text-slate-900 mb-6">
                99€ <span className="text-lg text-slate-500 font-medium">/ Monat</span>
              </div>
              <ul className="space-y-4 mb-8">
                <li className="flex items-center gap-3 text-slate-700"><CheckCircle2 className="w-5 h-5 text-red-600" /> 1 Apotheken-Standort</li>
                <li className="flex items-center gap-3 text-slate-700"><CheckCircle2 className="w-5 h-5 text-red-600" /> BYOD Patienten-Flow</li>
                <li className="flex items-center gap-3 text-slate-700"><CheckCircle2 className="w-5 h-5 text-red-600" /> WebRTC Videokonsultation</li>
                <li className="flex items-center gap-3 text-slate-700"><CheckCircle2 className="w-5 h-5 text-red-600" /> PDF/A Dokumentation</li>
              </ul>
              <Button className="w-full bg-slate-900 hover:bg-slate-800 text-white h-12 text-lg font-semibold rounded-lg">
                Demo vereinbaren
              </Button>
            </div>

            {/* Pro Tier */}
            <div className="bg-red-600 rounded-3xl p-8 shadow-xl border border-red-500 text-white relative transform md:-translate-y-4">
              <div className="absolute top-0 right-8 transform -translate-y-1/2">
                <span className="bg-emerald-400 text-slate-900 text-xs font-bold px-3 py-1 uppercase tracking-wider rounded-full">Beliebt</span>
              </div>
              <h3 className="text-2xl font-bold mb-2">Pro (Multi-Tenant)</h3>
              <p className="text-red-100 mb-6">Für Apothekenverbünde & externe Apotheker.</p>
              <div className="text-4xl font-extrabold mb-6">
                249€ <span className="text-lg text-red-200 font-medium">/ Monat</span>
              </div>
              <ul className="space-y-4 mb-8">
                <li className="flex items-center gap-3"><CheckCircle2 className="w-5 h-5 text-emerald-400" /> Bis zu 5 Standorte zentral verwalten</li>
                <li className="flex items-center gap-3"><CheckCircle2 className="w-5 h-5 text-emerald-400" /> Filialübergreifendes Routing</li>
                <li className="flex items-center gap-3"><CheckCircle2 className="w-5 h-5 text-emerald-400" /> Erweiterte Mandanten-Rollen (RBAC)</li>
                <li className="flex items-center gap-3"><CheckCircle2 className="w-5 h-5 text-emerald-400" /> Priority 24/7 Support</li>
              </ul>
              <Button className="w-full bg-white hover:bg-slate-100 text-red-600 h-12 text-lg font-bold rounded-lg shadow-md">
                Demo vereinbaren
              </Button>
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-slate-900 border-t border-slate-800 py-12 text-center text-slate-400">
        <div className="max-w-7xl mx-auto px-4">
          <p className="mb-4">© {new Date().getFullYear()} aTM.cloud Systems GmbH. Alle Rechte vorbehalten.</p>
          <div className="flex justify-center space-x-6 text-sm">
            <a href="#" className="hover:text-white transition-colors">Impressum</a>
            <a href="#" className="hover:text-white transition-colors">Datenschutz</a>
            <a href="#" className="hover:text-white transition-colors">AGB</a>
          </div>
        </div>
      </footer>
    </div>
  );
}
