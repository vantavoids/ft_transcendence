FROM node:22-alpine AS build
WORKDIR /app
ENV HUSKY=0

COPY package.json pnpm-lock.yaml ./
RUN corepack enable && corepack prepare pnpm@10 --activate
RUN pnpm install --frozen-lockfile

COPY . .

# browser-exposed TURN config, inlined into the bundle at build time.
# fed from the root .env (TURN_*) via frontend/compose.yaml build args.
ARG NEXT_PUBLIC_TURN_URL=
ARG NEXT_PUBLIC_TURN_USERNAME=
ARG NEXT_PUBLIC_TURN_PASSWORD=
ENV NEXT_PUBLIC_TURN_URL=$NEXT_PUBLIC_TURN_URL \
    NEXT_PUBLIC_TURN_USERNAME=$NEXT_PUBLIC_TURN_USERNAME \
    NEXT_PUBLIC_TURN_PASSWORD=$NEXT_PUBLIC_TURN_PASSWORD

RUN pnpm run build

FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV HOSTNAME=0.0.0.0
ENV PORT=3000

COPY --from=build /app/public ./public
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static

USER node
EXPOSE 3000
CMD ["node", "server.js"]
