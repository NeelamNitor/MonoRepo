// Minimal HS256 JWT generator for local smoke-testing (matches Auth:JwtSigningKey in appsettings.json).
const crypto = require('crypto');

const key = 'local-dev-signing-key-change-me-please-32bytes+';
const [, , sub, rolesArg] = process.argv;
const roles = (rolesArg || '').split(',').filter(Boolean);

const header = { alg: 'HS256', typ: 'JWT' };
const payload = {
  sub,
  role: roles.length === 1 ? roles[0] : roles,
  iat: Math.floor(Date.now() / 1000),
  exp: Math.floor(Date.now() / 1000) + 3600
};

const b64url = (obj) => Buffer.from(JSON.stringify(obj)).toString('base64url');
const unsigned = `${b64url(header)}.${b64url(payload)}`;
const signature = crypto.createHmac('sha256', key).update(unsigned).digest('base64url');
console.log(`${unsigned}.${signature}`);
