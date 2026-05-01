FROM node:20-alpine AS build
WORKDIR /app

COPY package.json .
COPY pnpm-lock.yaml .
RUN corepack enable && corepack prepare pnpm@latest --activate
RUN pnpm install --frozen-lockfile

COPY prisma/ prisma/
COPY prisma.config.ts .
RUN pnpm exec prisma generate

COPY tsconfig.json tsconfig.build.json ./
COPY src/ src/
RUN pnpm run build

FROM node:20-alpine AS runtime
RUN apk add --no-cache curl
WORKDIR /app

COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/generated ./generated
COPY --from=build /app/dist ./dist
COPY --from=build /app/prisma ./prisma
COPY package.json .
COPY prisma.config.ts .
COPY tsconfig.json .

EXPOSE 8080
CMD ["node", "dist/src/main"]
