# The web app as static files behind nginx, for running everything in Docker.
# Build from this folder: compose.yaml does it with `docker compose --profile app up -d --build`.

FROM node:24.18.0-alpine AS build
WORKDIR /app

# package files first: dependencies are rebuilt only when they change.
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.31.7-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/build/client /usr/share/nginx/html
