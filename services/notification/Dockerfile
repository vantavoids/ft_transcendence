# **************************************************************************** #
#                                                                              #
#                                                         :::      ::::::::    #
#    Dockerfile                                         :+:      :+:    :+:    #
#                                                     +:+ +:+         +:+      #
#    By: achu <achu@student.42.fr>                  +#+  +:+       +#+         #
#                                                 +#+#+#+#+#+   +#+            #
#    Created: 2026/03/16 17:14:00 by achu              #+#    #+#              #
#    Updated: 2026/04/03 21:57:38 by achu             ###   ########.fr        #
#                                                                              #
# **************************************************************************** #

FROM node:20-bookworm
WORKDIR /app

COPY prisma/ prisma/
COPY prisma.config.ts .
COPY package.json .
COPY tsconfig.json .
COPY tsconfig.build.json .

RUN npm install
RUN npx prisma generate

COPY src/ src/

EXPOSE "${APP_PORT}"

CMD ["sh", "-c", "npx prisma migrate deploy && npm run start:dev"]