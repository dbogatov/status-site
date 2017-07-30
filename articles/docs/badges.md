# Badges

You may put badges on your websites or in your markdown documents.

!!! success "Badge example"
    [![system health](https://status.dbogatov.org/health)](https://status.dbogatov.org/)

## Code

!!! warning
    Use **your domain** instead of `https://status.dbogatov.org`

### Markdown

Here is how to put **system health** badge in markdown

	[![system health](https://status.dbogatov.org/health)](https://status.dbogatov.org/)

Here is how to put **individual metric health** badge in markdown

	[![metric health](https://status.dbogatov.org/health/type/source)](https://status.dbogatov.org/home/metric/type/source)

Where *type* is a metric type (eq. `cpuload`) and *source* is a metric source.

### HTML

Here is how to put **system health** badge in markdown

	<a href="https://status.dbogatov.org/">
		<img alt="system health" src="https://status.dbogatov.org/health" />
	</a>

Here is how to put **individual metric health** badge in markdown

	<a href="https://status.dbogatov.org/home/metric/type/source">
		<img alt="metric health" src="https://status.dbogatov.org/health/type/source" />
	</a>

Where *type* is a metric type (eq. `cpuload`) and *source* is a metric source.
