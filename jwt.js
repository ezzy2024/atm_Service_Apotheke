const jwt = require('jsonwebtoken');
const token = jwt.sign({
    id: '999',
    role: 'Pharmacy',
    HasPremiumAccess: 'False'
}, 'vTccveQUGQTOL56EI0X/o3R1wHtjIjoed0NusZ9fKoY=', { expiresIn: '7d' });
console.log(token);
