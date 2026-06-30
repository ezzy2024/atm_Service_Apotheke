# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: e2e-workflow.spec.ts >> Automatisierter Kiosk-Triage und Export-Workflow
- Location: tests\e2e-workflow.spec.ts:3:1

# Error details

```
Test timeout of 30000ms exceeded.
```

```
Error: page.click: Test timeout of 30000ms exceeded.
Call log:
  - waiting for locator('text=Alle akzeptieren')

```

# Page snapshot

```yaml
- generic [ref=e3]:
  - banner [ref=e4]:
    - heading "Service Apotheke aTM" [level=1] [ref=e5]
  - main [ref=e6]:
    - generic [ref=e8]:
      - generic [ref=e9]:
        - 'heading "Schritt 1: Einverständniserklärung" [level=2] [ref=e11]'
        - button "Sitzung beenden" [ref=e12]
      - generic [ref=e14]:
        - generic [ref=e16]:
          - img [ref=e17]
          - generic [ref=e19]:
            - heading "Vereinbarung zur assistierten Telemedizin" [level=3] [ref=e20]
            - paragraph [ref=e21]: Ich willige in die Verarbeitung meiner Daten im Rahmen der assistierten Inanspruchnahme einer ambulanten telemedizinischen Leistung gegenüber der Apotheke ein. Diese Vereinbarung wird für 4 Jahre in der Apotheke aufbewahrt.
        - generic [ref=e22]:
          - heading "Patientendaten" [level=3] [ref=e23]
          - textbox "Vollständiger Name" [ref=e24]
          - generic [ref=e25]:
            - textbox "Krankenkasse Name" [ref=e27]
            - textbox "Kostenträgerkennung (IK - 9 Ziffern)" [ref=e29]
          - generic [ref=e30]:
            - textbox "Versichertennummer (KVNR - 1 Buchstabe, 9 Ziffern)" [ref=e32]
            - generic [ref=e33]:
              - generic [ref=e34]: Geburtsdatum
              - textbox [ref=e35]
          - generic [ref=e37]:
            - textbox "Statusfeld (erste 5 Ziffern von eGK)" [ref=e38]
            - generic [ref=e39]: "83"
        - generic [ref=e40]:
          - heading "Anspruchsvoraussetzungen" [level=3] [ref=e41]
          - paragraph [ref=e42]: "Bitte wählen Sie mindestens einen zutreffenden Grund:"
          - generic [ref=e43] [cursor=pointer]:
            - checkbox "Ich verfüge über kein geeignetes digitales Endgerät" [ref=e44]
            - generic [ref=e45]: Ich verfüge über kein geeignetes digitales Endgerät
          - generic [ref=e46] [cursor=pointer]:
            - checkbox "Dringender Fall (eigenes Gerät kann nicht benutzt werden)" [ref=e47]
            - generic [ref=e48]: Dringender Fall (eigenes Gerät kann nicht benutzt werden)
          - generic [ref=e49] [cursor=pointer]:
            - checkbox "Ich benötige praktische oder technische Hilfestellung" [ref=e50]
            - generic [ref=e51]: Ich benötige praktische oder technische Hilfestellung
        - generic [ref=e53]:
          - heading "Ihre digitale Unterschrift" [level=3] [ref=e54]
          - button "Löschen" [ref=e55]
        - button "Zustimmen & Fortfahren" [disabled]
```

# Test source

```ts
  1  | import { test, expect } from '@playwright/test';
  2  | 
  3  | test('Automatisierter Kiosk-Triage und Export-Workflow', async ({ page }) => {
  4  |   // 1. Kiosk Flow
  5  |   await page.goto('http://localhost:3000/kiosk/session/demo-session-1234');
  6  |   // Cookie Consent
> 7  |   await page.click('text=Alle akzeptieren');
     |              ^ Error: page.click: Test timeout of 30000ms exceeded.
  8  | 
  9  |   // Consent
  10 |   await page.click('text=eGK Einlesen'); 
  11 |   // Wait for mock read to complete (2 seconds)
  12 |   await page.waitForTimeout(2500);
  13 |   await page.click('text=Zustimmen & Fortfahren');
  14 | 
  15 |   // Service Selection
  16 |   await page.click('text=Workflow starten');
  17 |   
  18 |   // Disclaimer
  19 |   await page.click('text=Ich habe verstanden und bestätige'); 
  20 |   
  21 |   // SmED Simulation (Keine Red Flags)
  22 |   await page.click('text=Allgemeines Unwohlsein');
  23 |   
  24 |   // Notfallfrage (Nein) - The button has two lines, matching the substring "NEIN" is safer
  25 |   await page.locator('button:has-text("NEIN")').click();
  26 |   
  27 |   // Dauer
  28 |   await page.click('text=Weniger als 24 Stunden');
  29 | 
  30 |   // Result
  31 |   await page.click('text=Videosprechstunde starten');
  32 |   
  33 |   // Verify WebRTC loading screen
  34 |   await expect(page.locator('text=Verbindung wird aufgebaut')).toBeVisible();
  35 | 
  36 |   // 3. Admin Backend Verifikation (Bypass Login)
  37 |   await page.goto('http://localhost:3000/');
  38 |   await page.addInitScript(() => {
  39 |     window.localStorage.setItem('demo_pharmacy_id', 'test-pharmacy-uuid');
  40 |     window.localStorage.setItem('atm_cookie_consent', 'granted');
  41 |     // Inject Supabase session object if route guards depend on it
  42 |     window.localStorage.setItem('sb-tngpemwxwkwazijgivjn-auth-token', JSON.stringify({
  43 |       access_token: 'test-token',
  44 |       user: { id: 'test-user-id', role: 'authenticated' }
  45 |     }));
  46 |   });
  47 |   
  48 |   // Navigate to Dashboard
  49 |   await page.goto('http://localhost:3000/admin/dashboard');
  50 |   
  51 |   // 4. Export Trigger Test
  52 |   // Wait for the Dashboard to load and verify UI elements
  53 |   await expect(page.locator('text=Super-Admin Dashboard')).toBeVisible();
  54 |   
  55 |   const downloadPromise = page.waitForEvent('download');
  56 |   await page.click('text=Sonderbeleg-Daten exportieren');
  57 |   const download = await downloadPromise;
  58 |   expect(download.suggestedFilename()).toContain('.json');
  59 | });
  60 | 
```