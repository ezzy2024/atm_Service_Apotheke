const PDFDocument = require('pdfkit');
const fs = require('fs');
const path = require('path');

const outputDir = path.join(__dirname, '..', 'marketing');
if (!fs.existsSync(outputDir)) {
  fs.mkdirSync(outputDir);
}

// 1. Email Marketing PDF (Optional als Anhang)
function generateEmailPDF() {
  const doc = new PDFDocument({ margin: 50 });
  doc.pipe(fs.createWriteStream(path.join(outputDir, 'aTM_Email_Kampagne.pdf')));

  doc.fillColor('#0082C8').font('Helvetica-Bold').fontSize(24).text('Service Apotheke aTM', { align: 'center' });
  doc.moveDown(0.5);
  doc.fillColor('#64748b').font('Helvetica').fontSize(12).text('Assistierte Telemedizin für Ihre Offizin', { align: 'center' });
  doc.moveDown(2);

  doc.fillColor('#0f172a').font('Helvetica-Bold').fontSize(16).text('Telepharmazie-Gesetz ist in Kraft – Machen Sie Ihre Apotheke heute zur digitalen Filiale');
  doc.moveDown(1);
  
  doc.font('Helvetica').fontSize(11).text('Sehr geehrte(r) Herr/Frau Apothekeninhaber(in),');
  doc.moveDown(1);
  doc.text('heute tritt das neue Telepharmazie-Gesetz in Kraft. Ab sofort können Sie Patienten nicht nur vor Ort, sondern auch rechtssicher digital und am Kiosk-Terminal beraten – und diese Leistungen voll abrechnen (z.B. pDL).');
  doc.moveDown(1);
  
  doc.font('Helvetica-Bold').text('Mit Service Apotheke aTM bieten wir Ihnen das erste vollumfängliche "Doctolib für Apotheken":');
  doc.moveDown(0.5);
  doc.font('Helvetica').text('• Sofort einsatzbereites Kiosk-Terminal für Ihre Offizin (inkl. SmED-Triage).');
  doc.text('• Gesetzeskonforme Videosprechstunde (ABDA/LAK-konform, Ende-zu-Ende verschlüsselt).');
  doc.text('• Fertige AVV-Verträge und DSGVO-konformes Hosting.');
  doc.moveDown(1.5);
  
  doc.text('Lassen Sie uns in einem 10-minütigen Gespräch prüfen, wie wir das System diese Woche noch in Ihrer Apotheke installieren können.');
  doc.moveDown(2);
  
  doc.font('Helvetica-Bold').text('Mit freundlichen Grüßen,');
  doc.font('Helvetica').text('Ihr aTM-Team');
  doc.fillColor('#0082C8').text('www.service-apotheke.de');
  
  doc.end();
}

// 2. Postal Flyer PDF
function generateFlyerPDF() {
  const doc = new PDFDocument({ margin: 50 });
  doc.pipe(fs.createWriteStream(path.join(outputDir, 'aTM_Post_Flyer.pdf')));

  doc.fillColor('#0082C8').font('Helvetica-Bold').fontSize(28).text('Service Apotheke aTM', { align: 'center' });
  doc.moveDown(2);

  doc.fillColor('#e11d48').font('Helvetica-Bold').fontSize(20).text('Der Personalmangel kostet Sie jeden Tag Umsatz.', { align: 'center' });
  doc.fillColor('#0f172a').font('Helvetica-Bold').fontSize(16).text('Die Lösung: Das aTM Telepharmazie-Kiosk.', { align: 'center' });
  doc.moveDown(2);
  
  doc.font('Helvetica').fontSize(12).text('Sehr geehrte Kolleginnen und Kollegen,');
  doc.moveDown(1);
  doc.text('die Gesetzeslage hat sich heute geändert: Telepharmazie ist nun ein zentraler Baustein der modernen Patientenversorgung. Doch wie setzen Sie das ohne zusätzliches Personal um?');
  doc.moveDown(1.5);
  
  doc.font('Helvetica-Bold').fontSize(14).fillColor('#0082C8').text('Das aTM Kiosk-System übernimmt die Vorarbeit:');
  doc.moveDown(0.5);
  doc.font('Helvetica').fontSize(12).fillColor('#0f172a');
  doc.text('1. Patienten führen am Kiosk selbstständig eine KI-gestützte Ersteinschätzung durch.');
  doc.text('2. Das System erstellt ein fertiges Anamnese-Protokoll für Sie.');
  doc.text('3. Sie oder ein Remote-Apotheker schalten sich nur noch für die finale Beratung per Video dazu.');
  doc.moveDown(1.5);
  
  doc.font('Helvetica-Bold').fontSize(14).fillColor('#16a34a').text('Ihre Vorteile:');
  doc.moveDown(0.5);
  doc.font('Helvetica').fontSize(12).fillColor('#0f172a');
  doc.text('✅ BAK & LAK konform (inklusive Approbationsprüfung)');
  doc.text('✅ 100% DSGVO & E2E-verschlüsselt');
  doc.text('✅ Erschließung neuer Abrechnungsziffern ohne Personalaufbau');
  doc.moveDown(2);
  
  doc.rect(50, doc.y, 500, 80).fill('#f8fafc').stroke('#e2e8f0');
  doc.fillColor('#0f172a').font('Helvetica-Bold').text('Jetzt Partner-Apotheke werden:', 70, doc.y - 65);
  doc.font('Helvetica').text('Besuchen Sie unsere Website, um sich in 3 Minuten zu registrieren:', 70, doc.y + 5);
  doc.fillColor('#0082C8').font('Helvetica-Bold').text('https://atm-service-apotheke.onrender.com/register', 70, doc.y + 5);
  
  doc.end();
}

generateEmailPDF();
generateFlyerPDF();
console.log('Marketing PDFs generated in /marketing folder.');
