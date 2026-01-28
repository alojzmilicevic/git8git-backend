import type { FastifyInstance } from 'fastify';
import * as github from '../services/github.js';
import { decrypt } from '../services/crypto.js';
import type { UserRepository } from '../repositories/user.repository.js';

export function githubRoutes(userRepository: UserRepository) {
  return async function (fastify: FastifyInstance) {
    // All routes require authentication
    fastify.addHook('preHandler', fastify.authenticate);

    // Helper to get decrypted access token
    async function getAccessToken(userId: string): Promise<string> {
      const user = await userRepository.findById(userId);
      if (!user) {
        throw new Error('User not found');
      }
      return decrypt(user.accessToken);
    }

    const securitySchema = { security: [{ bearerAuth: [] }] };

    // List repositories
    fastify.get('/repos', { schema: securitySchema }, async (request, reply) => {
      try {
        const accessToken = await getAccessToken(request.user.userId);
        const repos = await github.listRepos(accessToken);
        return repos;
      } catch (err) {
        request.log.error(err);
        return reply.code(500).send({ error: 'Failed to fetch repositories' });
      }
    });

    // List branches
    fastify.get<{
      Params: { owner: string; repo: string };
    }>('/repos/:owner/:repo/branches', { schema: securitySchema }, async (request, reply) => {
      const { owner, repo } = request.params;

      try {
        const accessToken = await getAccessToken(request.user.userId);
        const branches = await github.listBranches(accessToken, owner, repo);
        return branches;
      } catch (err) {
        request.log.error(err);
        return reply.code(500).send({ error: 'Failed to fetch branches' });
      }
    });

    // Get file content
    fastify.get<{
      Params: { owner: string; repo: string; '*': string };
      Querystring: { ref?: string };
    }>('/repos/:owner/:repo/contents/*', { schema: securitySchema }, async (request, reply) => {
      const { owner, repo } = request.params;
      const path = request.params['*'];
      const { ref } = request.query;

      try {
        const accessToken = await getAccessToken(request.user.userId);
        const content = await github.getFileContent(accessToken, owner, repo, path, ref);

        if (!content) {
          return reply.code(404).send({ error: 'File not found' });
        }

        return content;
      } catch (err) {
        request.log.error(err);
        return reply.code(500).send({ error: 'Failed to fetch file content' });
      }
    });

    // Create or update file
    fastify.put<{
      Params: { owner: string; repo: string; '*': string };
      Body: {
        content: string;
        message: string;
        branch?: string;
        sha?: string;
      };
    }>('/repos/:owner/:repo/contents/*', { schema: securitySchema }, async (request, reply) => {
      const { owner, repo } = request.params;
      const path = request.params['*'];
      const { content, message, branch, sha } = request.body;

      if (!content || !message) {
        return reply.code(400).send({ error: 'content and message are required' });
      }

      try {
        const accessToken = await getAccessToken(request.user.userId);
        const result = await github.createOrUpdateFile(
          accessToken,
          owner,
          repo,
          path,
          content,
          message,
          branch,
          sha
        );

        return result;
      } catch (err) {
        request.log.error(err);
        return reply.code(500).send({ error: 'Failed to update file' });
      }
    });
  };
}
