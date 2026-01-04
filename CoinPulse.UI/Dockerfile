# 1. Build Aşaması (Node.js)
FROM node:18-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm install

COPY . .
# TypeScript derlemesi
RUN npm run build

# 2. Serve Aşaması (Nginx)
FROM nginx:alpine
# React build çıktılarını Nginx'in html klasörüne at
COPY --from=build /app/dist /usr/share/nginx/html

# Nginx config (React Router için gerekli - SPA)
RUN echo 'server { listen 80; location / { root /usr/share/nginx/html; try_files $uri $uri/ /index.html; } }' > /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]