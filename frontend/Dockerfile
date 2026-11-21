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
# The image's entrypoint renders templates/*.template into conf.d with the environment, and with
# NGINX_ENTRYPOINT_LOCAL_RESOLVERS it puts the DNS servers of /etc/resolv.conf in NGINX_LOCAL_RESOLVERS.
ENV NGINX_ENTRYPOINT_LOCAL_RESOLVERS=1 \
    API_UPSTREAM=http://api:8080
COPY nginx.conf /etc/nginx/templates/default.conf.template
COPY --from=build /app/build/client /usr/share/nginx/html
