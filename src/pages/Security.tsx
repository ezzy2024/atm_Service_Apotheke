import { Link } from "react-router-dom";
import { ShieldCheck, Database, Key, FileSignature, ArrowLeft } from "lucide-react";

export default function Security() {
  return (
    <div className="min-h-screen bg-white font-sans text-slate-900">
      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
        <Link to="/" className="inline-flex items-center text-slate-500 hover:text-red-600 mb-8 font-medium transition-colors">
          <ArrowLeft className="w-4 h-4 mr-2" /> Zurück zur Startseite
        </Link>
        
        <div className="flex items-center gap-4 mb-8">
          <div className="w-12 h-12 bg-red-50 rounded-xl flex items-center justify-center border border-red-100">
            <ShieldCheck className="w-6 h-6 text-red-600" />
          </div>
          <h1 className="text-4xl font-extrabold tracking-tight">Security & Compliance Hub</h1>
        </div>
        
        <p className="text-xl text-slate-500 mb-12 leading-relaxed">
          Transparenz ist die Grundlage für Vertrauen im Gesundheitswesen. Erfahren Sie hier im Detail, wie unsere Cloud-Architektur medizinische Daten schützt und DSGVO-konforme Telepharmazie ermöglicht.
        </p>
        
        <div className="space-y-12">
          {/* Section 1 */}
          <section className="bg-slate-50 rounded-3xl p-8 border border-slate-100">
            <div className="flex items-center gap-3 mb-4">
              <Key className="w-6 h-6 text-slate-700" />
              <h2 className="text-2xl font-bold">Mandanten-Isolation (RLS) & JWT</h2>
            </div>
            <p className="text-slate-600 leading-relaxed mb-4">
              Jede Apotheke erhält einen isolierten Datenraum ("Tenant"). Die Durchsetzung dieser Trennung erfolgt nicht nur auf Applikationsebene, sondern tief in der Datenbank durch PostgreSQL Row-Level-Security (RLS).
            </p>
            <ul className="list-disc list-inside text-slate-600 space-y-2">
              <li>API-Anfragen erfordern ein kryptografisch signiertes JSON Web Token (JWT).</li>
              <li>Ein Backend-Middleware prüft dynamisch die <code>user_pharmacy_roles</code>.</li>
              <li>Mutations-Abfragen auf fremde Apotheken (Cross-Tenant-Attacken) werden auf Datenbankebene hart blockiert (0 affected rows).</li>
            </ul>
          </section>

          {/* Section 2 */}
          <section className="bg-slate-50 rounded-3xl p-8 border border-slate-100">
            <div className="flex items-center gap-3 mb-4">
              <Database className="w-6 h-6 text-slate-700" />
              <h2 className="text-2xl font-bold">Infrastruktur & Hosting</h2>
            </div>
            <p className="text-slate-600 leading-relaxed mb-4">
              Das System ist vollständig "Cloud-native" konzipiert, verzichtet auf störanfällige lokale Kiosksysteme (BYOD-Pivot) und wird in zertifizierten Rechenzentren gehostet.
            </p>
            <ul className="list-disc list-inside text-slate-600 space-y-2">
              <li>End-to-End verschlüsselte Videokonsultationen via WebRTC (Datagram Transport Layer Security).</li>
              <li>Supabase Point-in-Time-Recovery (PITR) garantiert die lückenlose Wiederherstellbarkeit von Abrechnungsdaten.</li>
              <li>Hosting in ISO-27001 zertifizierten AWS/EU-Zonen (Frankfurt).</li>
            </ul>
          </section>

          {/* Section 3 */}
          <section className="bg-slate-50 rounded-3xl p-8 border border-slate-100">
            <div className="flex items-center gap-3 mb-4">
              <FileSignature className="w-6 h-6 text-slate-700" />
              <h2 className="text-2xl font-bold">PDF/A Audit-Architektur</h2>
            </div>
            <p className="text-slate-600 leading-relaxed mb-4">
              Revisionssicherheit ist essentiell für die Abrechnung von Telepharmazie-Leistungen (z.B. NNF-Erstattungen). 
            </p>
            <ul className="list-disc list-inside text-slate-600 space-y-2">
              <li>Klinische Anamnese-Protokolle werden serverseitig als PDF/A-Dokumente gerendert.</li>
              <li>Digitale Signaturen (Base64) werden fest im Dokument verankert.</li>
              <li>Speicherung erfolgt in einem rein privaten Supabase Storage Bucket (<code>billing_documentation</code>), auf den ohne JWT-Validierung kein Lesezugriff besteht.</li>
            </ul>
          </section>
        </div>
      </div>
    </div>
  );
}
