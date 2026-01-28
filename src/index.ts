import Fastify from 'fastify';
import cors from '@fastify/cors';
import swagger from '@fastify/swagger';
import swaggerUi from '@fastify/swagger-ui';
import { env } from './config/env.js';
import { authPlugin } from './plugins/auth.js';
import { authRoutes } from './routes/auth.js';
import { githubRoutes } from './routes/github.js';
import { SqliteUserRepository } from './repositories/sqlite-user.repository.js';

async function main() {
  const fastify = Fastify({
    logger: {
      level: env.isDev ? 'debug' : 'info',
    },
  });

  // Initialize repository
  const userRepository = new SqliteUserRepository();

  // CORS configuration
  const corsOrigins: (string | RegExp)[] = [];

  // Allow Chrome extension origin
  if (env.chromeExtensionId) {
    corsOrigins.push(`chrome-extension://${env.chromeExtensionId}`);
  }

  // In development, allow localhost
  if (env.isDev) {
    corsOrigins.push(/^http:\/\/localhost(:\d+)?$/);
  }

  await fastify.register(cors, {
    origin: corsOrigins.length > 0 ? corsOrigins : false,
    credentials: true,
  });

  // Swagger documentation
  await fastify.register(swagger, {
    openapi: {
      info: {
        title: 'Git8Git API',
        description: 'Backend API for Git8Git Chrome Extension',
        version: '1.0.0',
      },
      components: {
        securitySchemes: {
          bearerAuth: {
            type: 'http',
            scheme: 'bearer',
            bearerFormat: 'JWT',
          },
        },
      },
    },
  });

  await fastify.register(swaggerUi, {
    routePrefix: '/docs',
  });

  // Register plugins
  await fastify.register(authPlugin);

  // Register routes
  await fastify.register(authRoutes(userRepository), { prefix: '/auth' });
  await fastify.register(githubRoutes(userRepository), { prefix: '/api' });

  // Health check
  fastify.get('/health', async () => ({ status: 'ok' }));

  // Graceful shutdown
  const shutdown = async () => {
    fastify.log.info('Shutting down...');
    await fastify.close();
    userRepository.close();
    process.exit(0);
  };

  process.on('SIGINT', shutdown);
  process.on('SIGTERM', shutdown);

  // Start server
  try {
    await fastify.listen({ port: env.port, host: '0.0.0.0' });
    fastify.log.info(`Server running on http://localhost:${env.port}`);
    fastify.log.info(`Swagger UI available at http://localhost:${env.port}/docs`);
  } catch (err) {
    fastify.log.error(err);
    process.exit(1);
  }
}

main();
