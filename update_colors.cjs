const fs = require('fs');
const path = require('path');

const filePath = path.join(__dirname, 'src/pages/patient/Session.tsx');
let content = fs.readFileSync(filePath, 'utf8');

// Replace #003366 with red-600
content = content.replace(/bg-\[\#003366\]/g, 'bg-red-600');
content = content.replace(/text-\[\#003366\]/g, 'text-red-600');
content = content.replace(/ring-\[\#003366\]/g, 'ring-red-600');
content = content.replace(/accent-\[\#003366\]/g, 'accent-red-600');
content = content.replace(/border-\[\#003366\]/g, 'border-red-600');
content = content.replace(/bg-\[\#002244\]/g, 'bg-red-700');

// Replace blue-50/blue-100/blue-200 with red or slate variants
content = content.replace(/bg-blue-50/g, 'bg-red-50');
content = content.replace(/text-blue-900/g, 'text-red-900');
content = content.replace(/border-blue-200/g, 'border-red-200');
content = content.replace(/text-blue-600/g, 'text-red-600');

fs.writeFileSync(filePath, content, 'utf8');
console.log('Successfully updated Session.tsx aesthetics to Red Medical.');
