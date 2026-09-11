import { setupZonelessTestEnv } from 'jest-preset-angular/setup-env/zoneless';

setupZonelessTestEnv({
  errorOnUnknownElements: true,
  errorOnUnknownProperties: true
});

// Jest's jsdom environment doesn't implement crypto.subtle (unlike real browsers and Vitest's environment) —
// polyfill it from Node's built-in webcrypto so AuthService's dev-token minting (libs/shared/util/src/dev-auth.ts)
// works under test the same way it does at runtime.
import { webcrypto } from 'node:crypto';
if (!globalThis.crypto?.subtle) {
  Object.defineProperty(globalThis, 'crypto', { value: webcrypto, configurable: true });
}
