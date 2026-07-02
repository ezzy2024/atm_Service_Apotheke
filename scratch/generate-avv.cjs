const PDFDocument = require('pdfkit');
const fs = require('fs');

const doc = new PDFDocument({ margin: 50 });
doc.pipe(fs.createWriteStream('public/avv-vorlage.pdf'));

doc.font('Helvetica-Bold').fontSize(18).text('Auftragsverarbeitungsvertrag (AVV)', { align: 'center' });
doc.moveDown(1);

doc.font('Helvetica').fontSize(12).text('gemäß Art. 28 Datenschutz-Grundverordnung (DSGVO)', { align: 'center' });
doc.moveDown(2);

doc.font('Helvetica-Bold').fontSize(12).text('zwischen');
doc.moveDown(0.5);
doc.font('Helvetica').text('Service Apotheke aTM GmbH\n(nachfolgend "Auftragsverarbeiter")');
doc.moveDown(1);
doc.font('Helvetica-Bold').text('und');
doc.moveDown(0.5);
doc.font('Helvetica').text('Der angebundenen Apotheke (B2B-Partner)\n(nachfolgend "Verantwortlicher")');
doc.moveDown(2);

doc.font('Helvetica-Bold').fontSize(12).text('1. Gegenstand und Dauer des Auftrags');
doc.font('Helvetica').fontSize(10).text('Gegenstand des Auftrags ist die Bereitstellung des Kiosk-Terminals und der Assistierten Telemedizin (aTM). Der Auftragsverarbeiter verarbeitet personenbezogene Daten im Auftrag des Verantwortlichen. Die Dauer dieses Vertrages entspricht der Laufzeit der Hauptvereinbarung.');
doc.moveDown(1);

doc.font('Helvetica-Bold').fontSize(12).text('2. Art und Zweck der Verarbeitung');
doc.font('Helvetica').fontSize(10).text('Art der Verarbeitung: Erhebung, Speicherung und Übermittlung von Patientendaten, Triage-Daten und Videostreaming-Daten.\nZweck: Durchführung von Ersteinschätzungen (SmED) und Bereitstellung der Videosprechstunde.');
doc.moveDown(1);

doc.font('Helvetica-Bold').fontSize(12).text('3. Technische und organisatorische Maßnahmen (TOMs)');
doc.font('Helvetica').fontSize(10).text('Der Auftragsverarbeiter hat die Umsetzung der erforderlichen technischen und organisatorischen Maßnahmen vor Beginn der Verarbeitung dokumentiert (Anlage 1).');
doc.moveDown(3);

doc.font('Helvetica-Bold').fontSize(10).text('Bitte drucken Sie dieses Dokument aus, unterschreiben und stempeln Sie es, und laden Sie den Scan im Onboarding-Wizard hoch.', { align: 'center' });
doc.moveDown(4);

doc.moveTo(50, doc.y).lineTo(250, doc.y).stroke();
doc.text('Ort, Datum', 50, doc.y + 10);

doc.moveTo(300, doc.y - 10).lineTo(500, doc.y - 10).stroke();
doc.text('Unterschrift & Stempel der Apotheke', 300, doc.y + 10);

doc.end();
console.log('PDF AVV Vorlage generiert.');
