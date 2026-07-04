# --- Build ---
FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . ./
RUN npm run build

# --- Serve ---
FROM nginx:alpine
# nginx vervangt ${API_URL}/${API_HOST} bij het opstarten via envsubst.
COPY nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
