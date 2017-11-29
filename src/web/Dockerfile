FROM microsoft/dotnet:2.0.3-runtime

# Create directory for the app source code
WORKDIR /srv

# Copy the binary
COPY bin/release/netcoreapp2.0/publish/ /srv

# Expose the port and start the app
EXPOSE 5555

ENTRYPOINT /bin/bash -c "dotnet web.dll"
