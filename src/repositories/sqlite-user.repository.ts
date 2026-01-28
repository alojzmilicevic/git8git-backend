import Database from 'better-sqlite3';
import type { User } from '../types/index.js';
import type { UserRepository } from './user.repository.js';

export class SqliteUserRepository implements UserRepository {
  private db: Database.Database;

  constructor(dbPath: string = 'data/git8git.db') {
    this.db = new Database(dbPath);
    this.init();
  }

  private init(): void {
    this.db.exec(`
      CREATE TABLE IF NOT EXISTS users (
        userId TEXT PRIMARY KEY,
        username TEXT NOT NULL,
        email TEXT,
        avatarUrl TEXT,
        accessToken TEXT NOT NULL,
        refreshToken TEXT,
        createdAt TEXT NOT NULL,
        updatedAt TEXT NOT NULL
      )
    `);

    // Migration: add refreshToken column if it doesn't exist
    const columns = this.db.prepare("PRAGMA table_info(users)").all() as { name: string }[];
    if (!columns.some(col => col.name === 'refreshToken')) {
      this.db.exec('ALTER TABLE users ADD COLUMN refreshToken TEXT');
    }
  }

  async findById(userId: string): Promise<User | null> {
    const row = this.db
      .prepare('SELECT * FROM users WHERE userId = ?')
      .get(userId) as UserRow | undefined;

    if (!row) return null;

    return {
      userId: row.userId,
      username: row.username,
      email: row.email,
      avatarUrl: row.avatarUrl,
      accessToken: row.accessToken,
      refreshToken: row.refreshToken,
      createdAt: new Date(row.createdAt),
      updatedAt: new Date(row.updatedAt),
    };
  }

  async save(user: User): Promise<void> {
    const stmt = this.db.prepare(`
      INSERT INTO users (userId, username, email, avatarUrl, accessToken, refreshToken, createdAt, updatedAt)
      VALUES (?, ?, ?, ?, ?, ?, ?, ?)
      ON CONFLICT(userId) DO UPDATE SET
        username = excluded.username,
        email = excluded.email,
        avatarUrl = excluded.avatarUrl,
        accessToken = excluded.accessToken,
        refreshToken = excluded.refreshToken,
        updatedAt = excluded.updatedAt
    `);

    stmt.run(
      user.userId,
      user.username,
      user.email,
      user.avatarUrl,
      user.accessToken,
      user.refreshToken,
      user.createdAt.toISOString(),
      user.updatedAt.toISOString()
    );
  }

  async findByRefreshToken(refreshToken: string): Promise<User | null> {
    const row = this.db
      .prepare('SELECT * FROM users WHERE refreshToken = ?')
      .get(refreshToken) as UserRow | undefined;

    if (!row) return null;

    return {
      userId: row.userId,
      username: row.username,
      email: row.email,
      avatarUrl: row.avatarUrl,
      accessToken: row.accessToken,
      refreshToken: row.refreshToken,
      createdAt: new Date(row.createdAt),
      updatedAt: new Date(row.updatedAt),
    };
  }

  async delete(userId: string): Promise<void> {
    this.db.prepare('DELETE FROM users WHERE userId = ?').run(userId);
  }

  close(): void {
    this.db.close();
  }
}

interface UserRow {
  userId: string;
  username: string;
  email: string | null;
  avatarUrl: string | null;
  accessToken: string;
  refreshToken: string | null;
  createdAt: string;
  updatedAt: string;
}
