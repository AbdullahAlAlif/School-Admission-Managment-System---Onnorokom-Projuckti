
# ==========================================
# STAGE 1: BUILD REACT APPLICATION
# ==========================================

FROM node:24-alpine AS build

WORKDIR /app

# Copy dependency definitions.
COPY package*.json ./

# Install exact dependencies from package-lock.json.
RUN npm ci

# Copy the frontend source code.
COPY . .

# Backend URL is supplied at Docker build time.
ARG VITE_API_URL=http://localhost:8080

ENV VITE_API_URL=${VITE_API_URL}

# Generate the production React build.
RUN npm run build


# ==========================================
# STAGE 2: SERVE USING NGINX
# ==========================================

FROM nginx:stable-alpine AS final

# Copy compiled React files into Nginx.
COPY --from=build /app/dist /usr/share/nginx/html

# Replace default Nginx configuration.
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
