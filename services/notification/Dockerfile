# **************************************************************************** #
#                                                                              #
#                                                         :::      ::::::::    #
#    Dockerfile                                         :+:      :+:    :+:    #
#                                                     +:+ +:+         +:+      #
#    By: achu <achu@student.42.fr>                  +#+  +:+       +#+         #
#                                                 +#+#+#+#+#+   +#+            #
#    Created: 2026/03/16 17:14:00 by achu              #+#    #+#              #
#    Updated: 2026/03/17 02:34:45 by achu             ###   ########.fr        #
#                                                                              #
# **************************************************************************** #

FROM node:20-bookworm
WORKDIR /app

COPY src/ src/
COPY package.json .
COPY tsconfig.json .
COPY tsconfig.build.json .

RUN npm install

CMD ["npm", "run", "start:dev"]