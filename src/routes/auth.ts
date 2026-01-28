import type { FastifyInstance } from 'fastify';
import crypto from 'crypto';
import * as github from '../services/github.js';
import { encrypt } from '../services/crypto.js';
import type { UserRepository } from '../repositories/user.repository.js';
import type { User } from '../types/index.js';

// In-memory state store for CSRF protection
const stateStore = new Map<string, { createdAt: number }>();

// Access token expiry
const ACCESS_TOKEN_EXPIRY = '1h';

function generateRefreshToken(): string {
  return crypto.randomBytes(32).toString('hex');
}

// Clean up expired states (older than 10 minutes)
setInterval(() => {
  const now = Date.now();
  for (const [state, { createdAt }] of stateStore) {
    if (now - createdAt > 10 * 60 * 1000) {
      stateStore.delete(state);
    }
  }
}, 60 * 1000);

export function authRoutes(userRepository: UserRepository) {
  return async function (fastify: FastifyInstance) {
    // Initiate OAuth flow
    fastify.get('/github', async (request, reply) => {
      const state = crypto.randomBytes(16).toString('hex');
      stateStore.set(state, { createdAt: Date.now() });

      const authUrl = github.getAuthorizationUrl(state);
      return reply.redirect(authUrl);
    });

    // OAuth callback
    fastify.get<{
      Querystring: { code?: string; state?: string; error?: string };
    }>('/github/callback', async (request, reply) => {
      const { code, state, error } = request.query;

      if (error) {
        return reply.code(400).send({ error: `GitHub OAuth error: ${error}` });
      }

      if (!code || !state) {
        return reply.code(400).send({ error: 'Missing code or state' });
      }

      // Verify state
      if (!stateStore.has(state)) {
        return reply.code(400).send({ error: 'Invalid or expired state' });
      }
      stateStore.delete(state);

      try {
        // Exchange code for token
        const accessToken = await github.exchangeCodeForToken(code);

        // Get user info from GitHub
        const githubUser = await github.getUser(accessToken);

        // Save or update user
        const now = new Date();
        const existingUser = await userRepository.findById(String(githubUser.id));
        const refreshToken = generateRefreshToken();

        const user: User = {
          userId: String(githubUser.id),
          username: githubUser.login,
          email: githubUser.email,
          avatarUrl: githubUser.avatar_url,
          accessToken: encrypt(accessToken),
          refreshToken,
          createdAt: existingUser?.createdAt || now,
          updatedAt: now,
        };

        await userRepository.save(user);

        // Generate JWT access token
        const jwtAccessToken = fastify.jwt.sign(
          { userId: user.userId, username: user.username },
          { expiresIn: ACCESS_TOKEN_EXPIRY }
        );

        // Token data for extension
        const tokenData = {
          accessToken: jwtAccessToken,
          refreshToken,
          expiresIn: 3600, // 1 hour in seconds
        };

        // Return HTML that sends the tokens to the extension
        return reply.type('text/html').send(`
          <!DOCTYPE html>
          <html>
          <head>
            <title>Authentication Successful</title>
            <style>
              body {
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                display: flex;
                justify-content: center;
                align-items: center;
                height: 100vh;
                margin: 0;
                background: #f5f5f5;
              }
              .container {
                text-align: center;
                padding: 2rem;
                background: white;
                border-radius: 8px;
                box-shadow: 0 2px 10px rgba(0,0,0,0.1);
              }
              h1 { color: #28a745; }
            </style>
          </head>
          <body>
            <div class="container">
              <h1>✓ Authentication Successful</h1>
              <p>You can close this tab and return to the extension.</p>
            </div>
            <script>
              // Token data for extension
              const tokenData = ${JSON.stringify(tokenData)};
              
              // Try to communicate with extension via postMessage
              if (window.opener) {
                window.opener.postMessage({ type: 'GITHUB_AUTH_SUCCESS', ...tokenData }, '*');
              }
              
              // Store in URL hash for background script to read
              // Format: #accessToken=...&refreshToken=...&expiresIn=...
              const params = new URLSearchParams(tokenData);
              window.location.hash = params.toString();
            </script>
          </body>
          </html>
        `);
      } catch (err) {
        request.log.error(err);
        return reply.code(500).send({ error: 'Authentication failed' });
      }
    });

    // Get current user
    fastify.get(
      '/me',
      {
        preHandler: [fastify.authenticate],
        schema: {
          security: [{ bearerAuth: [] }],
        },
      },
      async (request, reply) => {
        const { userId } = request.user;

        const user = await userRepository.findById(userId);
        if (!user) {
          return reply.code(404).send({ error: 'User not found' });
        }

        return {
          userId: user.userId,
          username: user.username,
          email: user.email,
          avatarUrl: user.avatarUrl,
        };
      }
    );

    // Refresh access token
    fastify.post<{
      Body: { refreshToken: string };
    }>('/refresh', async (request, reply) => {
      const { refreshToken } = request.body;

      if (!refreshToken) {
        return reply.code(400).send({ error: 'refreshToken is required' });
      }

      // Find user by refresh token
      const user = await userRepository.findByRefreshToken(refreshToken);
      if (!user) {
        return reply.code(401).send({ error: 'Invalid refresh token' });
      }

      // Generate new access token
      const newAccessToken = fastify.jwt.sign(
        { userId: user.userId, username: user.username },
        { expiresIn: ACCESS_TOKEN_EXPIRY }
      );

      // Optionally rotate refresh token for extra security
      const newRefreshToken = generateRefreshToken();
      user.refreshToken = newRefreshToken;
      user.updatedAt = new Date();
      await userRepository.save(user);

      return {
        accessToken: newAccessToken,
        refreshToken: newRefreshToken,
        expiresIn: 3600, // 1 hour in seconds
      };
    });
  };
}
