FROM microsoft/dotnet:2.0.3-runtime

# Create directory for the app source code
WORKDIR /srv

# Copy the binary
COPY bin/release/netcoreapp2.0/publish/ /srv

RUN \
	rm -f /srv/appsettings.development.yml && \
	rm -f /srv/appsettings.testing.yml && \
	rm -f /srv/appsettings.production.yml

ENTRYPOINT /bin/bash -c "dotnet daemons.dll"
