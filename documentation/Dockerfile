FROM dbogatov/docker-containers:nginx-latest

MAINTAINER Dmytro Bogatov dmytro@dbogatov.org

WORKDIR /srv

# Copy the source
COPY out/ .

# Copy the NGINX config
COPY nginx.conf /etc/nginx/conf.d/default.conf

CMD ["nginx", "-g", "daemon off;"]
