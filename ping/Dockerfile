FROM alpine

# Create directory for the app source code
WORKDIR /srv

# Copy the binary
COPY bin/ping /srv

# Indicate the binary as our entrypoint
ENTRYPOINT /srv/ping

# Expose your port
EXPOSE 8888
