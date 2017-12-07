FROM alpine

# Install CA Certs for HTTPS
RUN apk update && apk add ca-certificates && rm -rf /var/cache/apk/*

# Create directory for the app source code
WORKDIR /srv

# Copy the binary
COPY bin/ping /srv

# Indicate the binary as our entrypoint
ENTRYPOINT /srv/ping
