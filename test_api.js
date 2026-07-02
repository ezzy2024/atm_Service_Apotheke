fetch('https://api.serviceapotheke.tech/api/Pharmacist/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email: 'test@test.com', password: 'wrong' })
}).then(r => {
  console.log('Status:', r.status);
  console.log('Set-Cookie:', r.headers.get('set-cookie'));
});
