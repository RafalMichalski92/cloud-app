# Etap 1: Budowanie aplikacji (Node.js)
FROM node:20-alpine AS build
WORKDIR /app
# Kopiujemy pliki konfiguracyjne npm
COPY package*.json ./
RUN npm install
# Kopiujemy resztę plików źródłowych
COPY . .
# Budujemy wersję produkcyjną
RUN npm run build

# Etap 2: Serwer Nginx (produkcja)
FROM nginx:alpine
# Kopiujemy ZBUDOWANĄ aplikację (folder dist) do folderu Nginxa
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]