const email = 'test' + Date.now() + '@test.com';
const password = 'Password123!';

async function run() {
  await fetch('https://api.serviceapotheke.tech/api/Pharmacist/register', {
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
  
  const loginRes = await fetch('https://api.serviceapotheke.tech/api/Pharmacist/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: email, password: password })
  });
  const cookie = loginRes.headers.get('set-cookie');
  console.log('Got cookie:', cookie);

  // Extract just the jwt=... part
  const jwtCookie = cookie.split(';')[0];
  
  const meRes = await fetch('https://api.serviceapotheke.tech/api/auth/me', {
    method: 'GET',
    headers: {
      'Cookie': jwtCookie
    }
  });
  console.log('Me Status:', meRes.status);
  const meText = await meRes.text();
  console.log('Me Body:', meText);
}
run();
