# jira-export

Work item migration tool that assists with moving Jira items to Azure DevOps or TFS.

    Usage: jira-export [options]

|Argument|Required|Description|
|---|---|---|
|-? \| -h \| --help|False|Show help information|
|-u \<username>|True|Username for authentication|
|-p \<password>|True|Password for authentication|
|--url \<accounturl>|True|Url for the account|
|--config \<configurationfilename>|True|Export the work items based on this configuration file|
|--force|False|Force execution from start (instead of continuing from previous run)|

## Example

```
jira-export -u myUser -p myPassword --url https://myproject.atlassian.net --config config.json --force
```
