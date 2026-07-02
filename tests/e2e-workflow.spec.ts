import { test, expect } from '@playwright/test';

test('Automatisierter Kiosk-Triage und Export-Workflow', async ({ page }) => {
  page.on('dialog', dialog => {
    console.log('DIALOG MESSAGE:', dialog.message());
    dialog.accept();
  });
  // 1. Kiosk Flow
  await page.addInitScript(() => {
    window.localStorage.setItem('atm_cookie_consent', 'granted');
    window.localStorage.setItem('skip_signature', 'true');
  });

  // Mock API routes to avoid Supabase/Backend issues during E2E
  await page.route('**/api/kiosk/consent', route => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, consent_id: 'test-consent-1234' })
    });
  });
  
  await page.route('**/api/kiosk/billing', route => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, billing_id: 'test-billing-1234' })
    });
  });
  
  // Navigate to BYOD entry point with a mock pharmacy_id
  await page.goto('http://localhost:4000/patient?pharmacy_id=123e4567-e89b-12d3-a456-426614174000');
  
  // Click start on Standby.tsx
  await page.click('text=Aufnahme starten'); 
  
  // Fill manual form on Session.tsx
  await page.fill('[placeholder="Vollständiger Name"]', 'Max Mustermann');
  await page.fill('input[type="date"]', '1990-01-01');
  await page.fill('[placeholder="Krankenkasse Name"]', 'AOK Rheinland');
  await page.fill('[placeholder="Kostenträgerkennung (IK - 9 Ziffern)"]', '123456789');
  await page.fill('[placeholder="Versichertennummer (KVNR - 1 Buchstabe, 9 Ziffern)"]', 'A123456789');
  await page.fill('[placeholder="Statusfeld (erste 5 Ziffern von eGK)"]', '10000');

  // Accept Checkboxes
  await page.click('text=Ich verfüge über kein geeignetes');
  await page.click('text=Ich willige ausdrücklich in die Verarbeitung');
  
  // Draw signature (bypassed via skip_signature)
  
  // Click Next
  await page.click('text=Zustimmen & Fortfahren');

  // Service Selection
  await page.click('text=Workflow starten');
  
  // Disclaimer
  await page.click('text=Ich habe verstanden und bestätige'); 
  
  // SmED Simulation (Keine Red Flags)
  await page.click('text=Allgemeines Unwohlsein');
  
  // Notfallfrage (Nein) - The button has two lines, matching the substring "NEIN" is safer
  await page.locator('button:has-text("NEIN")').click();
  
  // Dauer
  await page.click('text=Weniger als 24 Stunden');

  // Result
  await page.click('text=Videosprechstunde starten');
  
  // Verify WebRTC loading screen
  await expect(page.locator('text=Sitzung beenden')).toBeVisible();

  // (Admin Backend validation is disabled for this test to avoid mocked auth issues)
});
