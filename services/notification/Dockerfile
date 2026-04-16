FROM node:20-bookworm AS build
WORKDIR /app

COPY prisma/ prisma/
COPY prisma.config.ts .
COPY package.json .
COPY tsconfig.json .
COPY tsconfig.build.json .

RUN corepack enable && corepack prepare pnpm@latest --activate
RUN pnpm install
RUN pnpm exec prisma generate
COPY src/ src/
RUN pnpm run build

FROM node:20-bookworm-slim AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app

COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/dist ./dist
COPY --from=build /app/prisma ./prisma
COPY package.json .
COPY prisma.config.ts .
COPY tsconfig.json .

CMD ["pnpm", "run", "start:prod"]
