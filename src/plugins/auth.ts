import type { FastifyInstance, FastifyRequest, FastifyReply } from 'fastify';
import fastifyJwt from '@fastify/jwt';
import fp from 'fastify-plugin';
import { env } from '../config/env.js';

async function authPluginImpl(fastify: FastifyInstance) {
  await fastify.register(fastifyJwt, {
    secret: env.jwtSecret,
    sign: {
      expiresIn: '1h',
    },
  });

  // Decorator for protected routes
  fastify.decorate(
    'authenticate',
    async function (request: FastifyRequest, reply: FastifyReply) {
      try {
        await request.jwtVerify();
      } catch (err) {
        reply.code(401).send({ error: 'Unauthorized' });
      }
    }
  );
}

// Wrap with fp to break encapsulation - makes authenticate available to all routes
export const authPlugin = fp(authPluginImpl);

declare module 'fastify' {
  interface FastifyInstance {
    authenticate: (request: FastifyRequest, reply: FastifyReply) => Promise<void>;
  }
}
