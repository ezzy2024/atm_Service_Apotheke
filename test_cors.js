async function run() {
  const meRes = await fetch('https://api.serviceapotheke.tech/api/auth/me', {
    method: 'GET',
    headers: {
      'Origin': 'https://serviceapotheke.tech'
    }
  });
  console.log('Status:', meRes.status);
  for (const [key, val] of meRes.headers.entries()) {
    console.log(key, ':', val);
  }
}
run();
