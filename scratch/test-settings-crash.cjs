const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  let hasError = false;

  page.on('console', msg => {
    if (msg.type() === 'error') {
      console.log('PAGE ERROR:', msg.text());
      hasError = true;
    }
  });
  
  page.on('pageerror', error => {
    console.log('UNCAUGHT EXCEPTION:', error.message);
    hasError = true;
  });

  await page.addInitScript(() => {
    window.localStorage.setItem('demo_pharmacy_id', 'd3b07384-d113-4956-a50e-a1c563e4410a');
    window.localStorage.setItem('demo_role', 'pharmacy_admin');
  });

  console.log('Navigating to http://localhost:4000/admin/settings...');
  await page.goto('http://localhost:4000/admin/settings', { waitUntil: 'networkidle' });
  
  console.log('Clicking the AVV checkbox label...');
  await page.click('text="Auftragsverarbeitungsvertrag (AVV) akzeptieren"');
  
  console.log('Clicking the save button...');
  await page.click('text="Daten & AVV speichern"');
  
  await page.waitForTimeout(2000);
  
  if (!hasError) {
    console.log('SUCCESS: No crash occurred after accepting the AVV.');
  } else {
    console.log('FAILED: A crash occurred.');
  }

  await browser.close();
})();
