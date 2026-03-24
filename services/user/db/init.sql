-- users
CREATE TABLE users (
    id UUID PRIMARY KEY,
    auth_id UUID UNIQUE NOT NULL,
    username VARCHAR(32) UNIQUE NOT NULL,
    discriminator VARCHAR(4),

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMP NULL
);

-- profiles
CREATE TABLE profiles (
    user_id UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,

    display_name VARCHAR(64),
    bio TEXT,
    avatar_url TEXT,
    banner_url TEXT,

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- relationships
CREATE TABLE relationships (
    id UUID PRIMARY KEY,

    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    target_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,

    type VARCHAR(16) NOT NULL,
    status VARCHAR(16) NOT NULL,

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),

    UNIQUE(user_id, target_id)
);

-- user_settings
CREATE TABLE user_settings (
    user_id UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,

    theme VARCHAR(16) DEFAULT 'dark',
    locale VARCHAR(8) DEFAULT 'en',
    notifications_enabled BOOLEAN DEFAULT true,

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- indexes
CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_relationships_user ON relationships(user_id);
CREATE INDEX idx_relationships_target ON relationships(target_id);

-- seed users
INSERT INTO users (id, auth_id, username, discriminator)
VALUES
    ('11111111-1111-1111-1111-111111111111', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'alice', '0001'),
    ('22222222-2222-2222-2222-222222222222', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'bob', '0002'),
    ('33333333-3333-3333-3333-333333333333', 'cccccccc-cccc-cccc-cccc-cccccccccccc', 'carol', '0003');

INSERT INTO profiles (user_id, display_name, bio)
VALUES
    ('11111111-1111-1111-1111-111111111111', 'Alice', 'Hello from Alice'),
    ('22222222-2222-2222-2222-222222222222', 'Bob', 'Hello from Bob'),
    ('33333333-3333-3333-3333-333333333333', 'Carol', 'Hello from Carol');

INSERT INTO user_settings (user_id, theme, locale, notifications_enabled)
VALUES
    ('11111111-1111-1111-1111-111111111111', 'dark', 'en', true),
    ('22222222-2222-2222-2222-222222222222', 'light', 'en', true),
    ('33333333-3333-3333-3333-333333333333', 'dark', 'fr', false);
