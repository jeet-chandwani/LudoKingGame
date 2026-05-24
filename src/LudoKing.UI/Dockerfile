# Stage 1: build the Angular app
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npx ng build --configuration=production

# Stage 2: serve with nginx
FROM nginx:alpine
COPY --from=build /app/dist/ludo-king.ui/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
