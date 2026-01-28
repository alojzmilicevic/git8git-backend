export interface User {
  userId: string;
  username: string;
  email: string | null;
  avatarUrl: string | null;
  accessToken: string; // Encrypted GitHub token
  refreshToken: string | null; // For JWT refresh
  createdAt: Date;
  updatedAt: Date;
}

export interface GitHubUser {
  id: number;
  login: string;
  email: string | null;
  avatar_url: string;
}

export interface JWTPayload {
  userId: string;
  username: string;
}

declare module '@fastify/jwt' {
  interface FastifyJWT {
    payload: JWTPayload;
    user: JWTPayload;
  }
}

declare module 'fastify' {
  interface FastifyRequest {
    jwtPayload?: JWTPayload;
  }
}
