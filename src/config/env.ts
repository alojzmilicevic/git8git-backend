import 'dotenv/config';

function required(key: string): string {
  const value = process.env[key];
  if (!value) {
    throw new Error(`Missing required environment variable: ${key}`);
  }
  return value;
}

function optional(key: string, defaultValue: string): string {
  return process.env[key] || defaultValue;
}

export const env = {
  // GitHub OAuth
  githubClientId: required('GITHUB_CLIENT_ID'),
  githubClientSecret: required('GITHUB_CLIENT_SECRET'),
  githubCallbackUrl: required('GITHUB_CALLBACK_URL'),

  // Security
  jwtSecret: required('JWT_SECRET'),
  encryptionKey: required('ENCRYPTION_KEY'),

  // Extension
  chromeExtensionId: optional('CHROME_EXTENSION_ID', ''),

  // Server
  port: parseInt(optional('PORT', '3000'), 10),
  nodeEnv: optional('NODE_ENV', 'development'),

  get isDev() {
    return this.nodeEnv === 'development';
  },
};
