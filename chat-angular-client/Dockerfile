# Stage 1: Build the Angular app
FROM node:20-alpine AS builder
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build -- --output-path=dist

# Stage 2: Run Angular dev server
FROM node:20-alpine
WORKDIR /app
COPY --from=builder /app .
EXPOSE 4200
CMD ["npx", "ng", "serve", "--host", "0.0.0.0", "--port", "4200"] 