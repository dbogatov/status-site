<!-- cSpell:ignore Kubernetes kubectl -->

# Deployment

## Kubernetes

Officially supported deployment strategy is using Kubernetes.
Please, see [Kubernetes docs](https://kubernetes.io/).
I have to assume some basic experience with Kubernetes.

You can deploy most of the system right away with the following command

	kubectl apply -f https://git.dbogatov.org/dbogatov/status-site/-/jobs/artifacts/master/raw/deployment/config.yaml?job=release-deployment

You also need to supply your settings

	kubectl create secret -n status-site generic appsettings.production.yml --from-file=/path/to/config.yaml

where your settings reside in `/path/to/config.yaml`.

This will download the config file from CI and apply it against your cluster.
It is recommended to inspect the file before applying it for security considerations.

This config creates a namespace `status-site` and creates resources within it.
It creates tuples deployment-service for each part of the system.
Components are designed to communicate with each other by names provided in config.
Each component also gets `appsettings` secret mounted as volume.

Lastly, database needs a stable persistent storage, so config include Persistent Volume Claim for 10 GB.
It is cluster administrator's responsibility to provide Volumes that meet claims.
You may adjust the claim's capacity if needed.

Finally, you may want to add ingress to make website accessible from the world.

By default, all services are not replicated (i.e. replication factor 1).
You may want to adjust this parameter.
Be careful, though.
The only components that are safe to replicate are `docs`, `ping` and `web`.

If `web` is replicated you may have issues with authorization, as one replica generated the cookie and another one does not recognize it.
I am working on this issue.

## LEGACY: Deploy to swarm (preferred way)

> This is the old way to deploy the system.
> It is not officially supported, but will likely work.

Status site is designed with swarm in mind.
The preferred way to deploy the system is using `docker stack deploy` command.

### TL;DR
	
	#!bash

	# init swarm if necessary
	docker swarm init
	
	# download and set configuration
	curl -L -o appsettings.production.yml https://git.dbogatov.org/dbogatov/status-site/-/jobs/artifacts/master/raw/appsettings.production.yml?job=release-app-docs
	docker secret create appsettings.production.yml appsettings.production.yml

	# download compose file
	curl -L -o docker-compose.yml https://git.dbogatov.org/dbogatov/status-site/-/jobs/artifacts/master/raw/docker-compose.yml?job=release-app-docs

	# deploy stack
	docker stack deploy --compose-file docker-compose.yml status

	# if you want to bind to port 80
	docker service update status_nginx --publish-add 80:80

	# if you want to join existing docker network
	docker service update status_nginx --network-add my-overlay

	# verify your deployment
	docker stack services status


### Prerequisites

You need the following before you can deploy a stack into swarm.

* Your docker node has to operate in [*swarm* mode](https://docs.docker.com/engine/swarm/).
* You have to have one *secret* in your swarm - [app config](/configuration/).
* You have to have `docker-compose-yml` file which defines the stack.
* By default, the stack does not open up ports (eq. 80) because it is designed to be a part of an existing infrastructure.
You need to manually either open a port, or hook existing reverse proxy to the stack.

Here is the explanation of each of these prerequisites.

In general, it does not hurt to convert a regular node to a swarm node (size 1 cluster).
General command is `docker swarm init`.
If you want a truly **highly available** multi-node cluster, you might want to setup a number of nodes.
Please, refer to [docker swarm documentation](https://docs.docker.com/engine/swarm/) for instructions.

The stack requires one secret - [app config](/configuration/).
You may download up-to-date example config file [here](https://git.dbogatov.org/dbogatov/status-site/-/jobs/artifacts/master/raw/appsettings.production.yml?job=release-app-docs).
Please, refer to [Configuration section](/configuration/) for config explanation.
Once you have the config (eq. `appsettings.production.yml`), run this command `docker secret create appsettings.production.yml appsettings.production.yml`

!!! warning
    PostgreSQL database connection string is hardwired into the application.
	It may be changed, though, by manually editing `appsettings.yml` and `docker-compose.yml`.
	The security relies on internal docker network created for the stack, so nobody can even access database from the outside.
	See up-to-date connection string in [Configuration section](/configuration/).

`docker-compose.yml` is not intended to be modified.
Download latest version [here](https://git.dbogatov.org/dbogatov/status-site/-/jobs/artifacts/master/raw/docker-compose.yml?job=release-app-docs)

At this point, you are ready to deploy the stack!

	#!bash
	docker stack deploy --compose-file docker-compose.yml status

If you want to serve the website on the node where you are deploying the stack, open up ports for **nginx** service of the stack **after** you deploy the stack.

	#!bash
	docker service update status_nginx --publish-add 80:80

If you want to add stack to an existing docker network, run the following

	#!bash
	docker service update status_nginx --network-add my-overlay

You are all set!
Run `docker stack services status` to verify your deployment.

!!! tip
	Debian package is under construction, which will automate these tasks for you.

## Other deployment strategies (on you own risk)

It is possible to run stack as a *docker composition* (using the same `docker-compose.yml` file).
You might need to modify composition file a little.

It is also possible to run composition containers manually.

Finally, it is possible to build the app from source and serve it from the bare metal.
