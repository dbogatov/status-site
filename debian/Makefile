install:
	install -D -m 0755 status-ctl.sh $$(pwd)/debian/status-ctl/usr/bin/status-ctl
	mkdir -p $$(pwd)/debian/status-ctl/etc/status-site/
	cp --no-preserve=ownership appsettings.production.yml $$(pwd)/debian/status-ctl/etc/status-site/appsettings.yml
	cp --no-preserve=ownership docker-compose.yml $$(pwd)/debian/status-ctl/etc/status-site/docker-compose.yml
	cp --no-preserve=ownership .env $$(pwd)/debian/status-ctl/etc/status-site/.env
