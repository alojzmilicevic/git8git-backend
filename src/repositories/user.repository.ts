import type { User } from '../types/index.js';

export interface UserRepository {
  findById(userId: string): Promise<User | null>;
  findByRefreshToken(refreshToken: string): Promise<User | null>;
  save(user: User): Promise<void>;
  delete(userId: string): Promise<void>;
}
