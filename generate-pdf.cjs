const PDFDocument = require('pdfkit');
const fs = require('fs');

const doc = new PDFDocument();
doc.pipe(fs.createWriteStream('public/avv-vorlage.pdf'));

doc.fontSize(25).text('Auftragsverarbeitungsvertrag (AVV)', { align: 'center' });
doc.moveDown();
doc.fontSize(12).text('Dies ist eine Dummy-Vorlage fuer den Auftragsverarbeitungsvertrag (AVV) zwischen der Service Apotheke und der angebundenen Apotheke (B2B Kunde).');
doc.moveDown();
doc.text('Bitte drucken Sie dieses Dokument aus, unterschreiben Sie es und laden Sie es im Onboarding-Wizard wieder hoch.');
doc.moveDown(5);
doc.text('_________________________________________________');
doc.text('Ort, Datum, Unterschrift Apotheken-Administrator');

doc.end();
console.log('PDF generated successfully.');
