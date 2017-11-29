FROM microsoft/dotnet:2.0.3-runtime

# Create directory for the app source code
WORKDIR /srv

# Copy the binary
COPY bin/release/netcoreapp2.0/publish/ /srv

ENTRYPOINT /bin/bash -c "dotnet daemons.dll"
