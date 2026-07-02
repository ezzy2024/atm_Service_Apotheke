const email = 'test' + Date.now() + '@test.com';
const password = 'Password123!';

async function run() {
  const regRes = await fetch('https://api.serviceapotheke.tech/api/Pharmacist/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      fullName: 'Test User',
      email: email,
      password: password,
      phoneNumber: '123456789',
      address: 'Test Str 1',
      qualification: 'Approbation',
      wwsProficiency: 'Test'
    })
  });
  console.log('Register Status:', regRes.status);
  
  const loginRes = await fetch('https://api.serviceapotheke.tech/api/Pharmacist/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: email, password: password })
  });
  console.log('Login Status:', loginRes.status);
  console.log('Login Set-Cookie:', loginRes.headers.get('set-cookie'));
}
run();
