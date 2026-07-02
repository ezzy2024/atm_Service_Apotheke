FROM node:20-alpine AS builder

WORKDIR /app

# Install dependencies
COPY package.json package-lock.json ./
RUN npm ci

# Copy source and build
COPY . .
RUN npm run build

# Production Image
FROM node:20-alpine

WORKDIR /app

# Install PM2 globally
RUN npm install -g pm2

# Copy built assets and PM2 config
COPY --from=builder /app/dist ./dist
COPY --from=builder /app/package.json ./package.json
COPY --from=builder /app/package-lock.json ./package-lock.json
COPY ecosystem.config.cjs ./

# Install only production dependencies
RUN npm ci --omit=dev

# Expose internal port
EXPOSE 4000

# Start via PM2
CMD ["pm2-runtime", "start", "ecosystem.config.cjs"]
