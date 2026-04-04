-- CreateEnum
CREATE TYPE "NotificationType" AS ENUM ('mention', 'dm', 'friend_request', 'guild_invite', 'guild_welcome', 'incoming_call');

-- CreateEnum
CREATE TYPE "ScopeType" AS ENUM ('guild', 'channel');

-- CreateTable
CREATE TABLE "notifications" (
    "id" BIGINT NOT NULL,
    "user_id" BIGINT NOT NULL,
    "type" "NotificationType" NOT NULL,
    "actor_id" BIGINT,
    "source_id" BIGINT,
    "payload" JSONB NOT NULL DEFAULT '{}',
    "read" BOOLEAN NOT NULL DEFAULT false,
    "dismissed_at" TIMESTAMP(3),
    "expires_at" TIMESTAMP(3),
    "created_at" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "notifications_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "notification_preferences" (
    "user_id" BIGINT NOT NULL,
    "scope_type" "ScopeType" NOT NULL,
    "scope_id" BIGINT NOT NULL,
    "muted" BOOLEAN NOT NULL DEFAULT false,
    "muted_until" TIMESTAMP(3),

    CONSTRAINT "notification_preferences_pkey" PRIMARY KEY ("user_id","scope_type","scope_id")
);

-- CreateIndex (partial)
CREATE INDEX "idx_notifications_user_unread" ON "notifications"("user_id", "created_at" DESC)
    WHERE read = FALSE AND dismissed_at IS NULL;

-- CreateIndex
CREATE INDEX "idx_notifications_user_all" ON "notifications"("user_id", "created_at" DESC);

-- CreateIndex
CREATE INDEX "idx_notifications_user_type" ON "notifications"("user_id", "type", "created_at" DESC);

-- CreateIndex
CREATE INDEX "idx_notifications_actor" ON "notifications"("actor_id");

-- CreateIndex
CREATE INDEX "idx_notif_prefs_scope" ON "notification_preferences"("scope_type", "scope_id");
