# jira-export

Work item migration tool that assists with moving Jira items to Azure DevOps or TFS.

```txt
Usage: jira-export [options]
```

|Argument|Required|Description|
|---|---|---|
|-? \| -h \| --help|False|Show help information|
|-u \<username>|True|Username for authentication|
|-p \<password>|True|Password for authentication|
|-t \<token>|False|OAuth 2.0 token (leave empty unless authenticating with OAuth)|
|--url \<jira url>|True|Url of the Jira organization|
|--config \<configuration filename>|True|Export the work items based on this configuration file|
|--force|False|Force execution from start (instead of continuing from previous run)|

## Examples

### Usage, authentication with username + password

```bash
.\jira-export.exe -u myUser -p myPassword --url https://myorganization.atlassian.net --config config.json --force
```

### Usage, authentication with OAuth2 token

```bash
.\jira-export.exe -u myUser -p myPassword -t myToken --url https://myorganization.atlassian.net --config config.json --force
```
