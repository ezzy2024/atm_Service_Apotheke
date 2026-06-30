const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  page.on('console', msg => {
    if (msg.type() === 'error') console.log('PAGE ERROR:', msg.text());
  });
  
  page.on('pageerror', err => {
    console.log('PAGE EXCEPTION:', err.message);
  });

  // Inject login session
  await page.addInitScript(() => {
    window.localStorage.setItem('demo_pharmacy_id', 'test-pharmacy-uuid');
    window.localStorage.setItem('demo_role', 'pharmacist');
    window.localStorage.setItem('demo_pharmacist_name', 'Test');
    window.localStorage.setItem('atm_cookie_consent', 'granted');
  });

  await page.goto('http://localhost:4000/admin/settings');
  await page.waitForTimeout(2000);
  const html = await page.evaluate(() => document.body.innerHTML);
  require('fs').writeFileSync('scratch/dom.html', html);
  await browser.close();
})();
