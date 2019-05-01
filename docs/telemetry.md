# Telemetry

## Usage Tracking

We use [Application Insights]() to collect usage and error information in order to improve the quality of the tools.

Currently we collect the following anonymous data:

* Event data: application version, client city/country, hosting type, item count, error count, warning count, elapsed time.
* Dependencies: REST calls to Jira and Azure DevOps to help us understand performance issues.

All logging logic can be review in the 'src/WorkItemMigrator/Migration.Common.Log/Logger.cs' source file.

## Opt out of tracking

If you want to opt out of us collecting telemetry you just need to remove the application insights key from the application config files:

```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  ...
  <appSettings>
    <add key="applicationInsightsKey" value="__APP_INSIGHTS_KEY__" /> 
  </appSettings>
</configuration>
```
