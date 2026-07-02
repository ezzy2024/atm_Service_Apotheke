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

  const routes = [
    'http://localhost:4000/',
    'http://localhost:4000/admin',
    'http://localhost:4000/admin/settings'
  ];

  for (const route of routes) {
    console.log(`\nNavigating to ${route}...`);
    await page.goto(route, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
  }
  
  if (!hasError) {
    console.log('\nSUCCESS: No crashes occurred on any main routes.');
  } else {
    console.log('\nFAILED: A crash occurred during the test.');
  }

  await browser.close();
})();
