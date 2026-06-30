import { test, expect } from '@playwright/test';

test('Automatisierter Kiosk-Triage und Export-Workflow', async ({ page }) => {
  // 1. Kiosk Flow
  await page.goto('http://localhost:3000/kiosk/session/demo-session-1234');
  
  // Consent
  await page.click('text=eGK Einlesen'); 
  // Wait for mock read to complete (2 seconds)
  await page.waitForTimeout(2500);
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
  await expect(page.locator('text=Verbindung wird aufgebaut')).toBeVisible();

  // 3. Admin Backend Verifikation (Bypass Login)
  await page.goto('http://localhost:3000/');
  await page.addInitScript(() => {
    window.localStorage.setItem('demo_pharmacy_id', 'test-pharmacy-uuid');
    // Inject Supabase session object if route guards depend on it
    window.localStorage.setItem('sb-tngpemwxwkwazijgivjn-auth-token', JSON.stringify({
      access_token: 'test-token',
      user: { id: 'test-user-id', role: 'authenticated' }
    }));
  });
  
  // Navigate to Dashboard
  await page.goto('http://localhost:3000/admin/dashboard');
  
  // 4. Export Trigger Test
  // Wait for the Dashboard to load and verify UI elements
  await expect(page.locator('text=Super-Admin Dashboard')).toBeVisible();
  
  const downloadPromise = page.waitForEvent('download');
  await page.click('text=Sonderbeleg-Daten exportieren');
  const download = await downloadPromise;
  expect(download.suggestedFilename()).toContain('.json');
});
