// Local-dev stand-in for a real OAuth2/OIDC login (research.md item 5 defers IdP selection). Mints an HS256
// JWT client-side using the SAME shared signing key both backend services trust ("Auth:JwtSigningKey" in
// their appsettings.json), so the UI can authenticate against the real APIs without a full IdP wired up.
// Swap this module for a real OIDC client once an identity provider is chosen — nothing else in either app
// depends on tokens coming from here specifically, only on getAuthToken() returning a bearer token string.

const DEV_SIGNING_KEY = 'local-dev-signing-key-change-me-please-32bytes+';

function base64url(bytes: Uint8Array): string {
  let binary = '';
  bytes.forEach((b) => (binary += String.fromCharCode(b)));
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function base64urlFromString(input: string): string {
  return base64url(new TextEncoder().encode(input));
}

async function hmacSha256(key: string, data: string): Promise<Uint8Array> {
  const cryptoKey = await crypto.subtle.importKey(
    'raw',
    new TextEncoder().encode(key),
    { name: 'HMAC', hash: 'SHA-256' },
    false,
    ['sign']
  );
  const signature = await crypto.subtle.sign('HMAC', cryptoKey, new TextEncoder().encode(data));
  return new Uint8Array(signature);
}

export interface DevUser {
  userId: string;
  roles: string[];
}

export async function createDevToken(user: DevUser): Promise<string> {
  const header = { alg: 'HS256', typ: 'JWT' };
  const nowSeconds = Math.floor(Date.now() / 1000);
  const payload = {
    sub: user.userId,
    role: user.roles.length === 1 ? user.roles[0] : user.roles,
    iat: nowSeconds,
    exp: nowSeconds + 60 * 60,
  };

  const unsigned = `${base64urlFromString(JSON.stringify(header))}.${base64urlFromString(JSON.stringify(payload))}`;
  const signature = await hmacSha256(DEV_SIGNING_KEY, unsigned);
  return `${unsigned}.${base64url(signature)}`;
}

const STORAGE_KEY = 'dev-auth-user';

export function getStoredDevUser(): DevUser | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as DevUser) : null;
  } catch {
    return null;
  }
}

export function storeDevUser(user: DevUser): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
}
