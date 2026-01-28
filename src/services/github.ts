import { Octokit } from 'octokit';
import { env } from '../config/env.js';
import type { GitHubUser } from '../types/index.js';

const GITHUB_OAUTH_URL = 'https://github.com/login/oauth';
const SCOPES = ['repo', 'read:user', 'user:email'];

export function getAuthorizationUrl(state: string): string {
  const params = new URLSearchParams({
    client_id: env.githubClientId,
    redirect_uri: env.githubCallbackUrl,
    scope: SCOPES.join(' '),
    state,
  });

  return `${GITHUB_OAUTH_URL}/authorize?${params.toString()}`;
}

export async function exchangeCodeForToken(code: string): Promise<string> {
  const response = await fetch(`${GITHUB_OAUTH_URL}/access_token`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json',
    },
    body: JSON.stringify({
      client_id: env.githubClientId,
      client_secret: env.githubClientSecret,
      code,
    }),
  });

  const data = (await response.json()) as { access_token?: string; error?: string };

  if (data.error || !data.access_token) {
    throw new Error(`GitHub OAuth error: ${data.error || 'No access token received'}`);
  }

  return data.access_token;
}

export async function getUser(accessToken: string): Promise<GitHubUser> {
  const octokit = new Octokit({ auth: accessToken });
  const { data } = await octokit.rest.users.getAuthenticated();

  return {
    id: data.id,
    login: data.login,
    email: data.email,
    avatar_url: data.avatar_url,
  };
}

export function createOctokit(accessToken: string): Octokit {
  return new Octokit({ auth: accessToken });
}

// Repository operations
export async function listRepos(accessToken: string) {
  const octokit = createOctokit(accessToken);
  const { data } = await octokit.rest.repos.listForAuthenticatedUser({
    sort: 'updated',
    per_page: 100,
  });

  return data.map((repo) => ({
    id: repo.id,
    name: repo.name,
    fullName: repo.full_name,
    owner: repo.owner.login,
    private: repo.private,
    defaultBranch: repo.default_branch,
  }));
}

export async function listBranches(accessToken: string, owner: string, repo: string) {
  const octokit = createOctokit(accessToken);
  const { data } = await octokit.rest.repos.listBranches({
    owner,
    repo,
    per_page: 100,
  });

  return data.map((branch) => ({
    name: branch.name,
    protected: branch.protected,
  }));
}

export async function getFileContent(
  accessToken: string,
  owner: string,
  repo: string,
  path: string,
  ref?: string
) {
  const octokit = createOctokit(accessToken);

  try {
    const { data } = await octokit.rest.repos.getContent({
      owner,
      repo,
      path,
      ref,
    });

    if (Array.isArray(data)) {
      // It's a directory
      return {
        type: 'directory' as const,
        items: data.map((item) => ({
          name: item.name,
          path: item.path,
          type: item.type,
          sha: item.sha,
        })),
      };
    }

    if (data.type !== 'file' || !('content' in data)) {
      throw new Error('Not a file');
    }

    return {
      type: 'file' as const,
      content: Buffer.from(data.content, 'base64').toString('utf-8'),
      sha: data.sha,
      path: data.path,
    };
  } catch (error: unknown) {
    if (error instanceof Error && 'status' in error && error.status === 404) {
      return null;
    }
    throw error;
  }
}

export async function createOrUpdateFile(
  accessToken: string,
  owner: string,
  repo: string,
  path: string,
  content: string,
  message: string,
  branch?: string,
  sha?: string
) {
  const octokit = createOctokit(accessToken);

  // Check if content is unchanged by comparing with existing file
  if (sha) {
    const existing = await getFileContent(accessToken, owner, repo, path, branch);
    if (existing && existing.type === 'file' && existing.content === content) {
      return {
        changed: false,
        sha: existing.sha,
        path: existing.path,
      };
    }
  }

  const { data } = await octokit.rest.repos.createOrUpdateFileContents({
    owner,
    repo,
    path,
    message,
    content: Buffer.from(content).toString('base64'),
    branch,
    sha,
  });

  return {
    changed: true,
    sha: data.content?.sha,
    path: data.content?.path,
  };
}
