export class PrintService {
  /**
   * Silently prints the provided HTML content using an invisible iframe.
   * Relies on the browser being started in Kiosk mode (e.g., Chrome --kiosk-printing)
   * to bypass the print dialog.
   */
  static printHtml(htmlContent: string): void {
    const iframe = document.createElement('iframe');
    iframe.style.position = 'fixed';
    iframe.style.right = '0';
    iframe.style.bottom = '0';
    iframe.style.width = '0';
    iframe.style.height = '0';
    iframe.style.border = '0';
    document.body.appendChild(iframe);

    const doc = iframe.contentWindow?.document;
    if (doc) {
      doc.open();
      doc.write(htmlContent);
      doc.close();

      // Ensure content is loaded before printing
      iframe.onload = () => {
        iframe.contentWindow?.focus();
        iframe.contentWindow?.print();
        
        // Cleanup after printing
        setTimeout(() => {
          document.body.removeChild(iframe);
        }, 1000);
      };
    }
  }

  /**
   * Generates the receipt HTML for the patient
   */
  static generateReceiptHtml(patientName: string, service: string): string {
    const date = new Date().toLocaleString('de-DE');
    return `
      <!DOCTYPE html>
      <html>
        <head>
          <style>
            body { font-family: monospace; font-size: 14px; padding: 20px; text-align: center; }
            h2 { font-size: 18px; margin-bottom: 10px; }
            .divider { border-bottom: 1px dashed #000; margin: 15px 0; }
            .footer { font-size: 12px; margin-top: 20px; }
          </style>
        </head>
        <body>
          <h2>Service Apotheke</h2>
          <p>Telepharmazie-Terminal</p>
          <div class="divider"></div>
          <p><strong>Beleg / Protokoll</strong></p>
          <p>Patient: ${patientName}</p>
          <p>Leistung: ${service}</p>
          <p>Datum: ${date}</p>
          <div class="divider"></div>
          <p class="footer">Bitte legen Sie diesen Beleg bei der Rezeptausgabe vor.</p>
        </body>
      </html>
    `;
  }
}
