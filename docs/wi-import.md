# wi-import

Work item migration tool that assists with moving Jira items to Azure DevOps or TFS.

    Usage: wi-import [options]

|Argument|Required|Description|
|---|---|---|
|-? \| -h \| --help|False|Show help information|
|--token \<accesstoken>|True|Personal access token to use for authentication|
|--url \<accounturl>|True|Url for the account|
|--config \<configurationfilename>|True|Import the work items based on the configuration file|
|--force|False|Force execution from start (instead of continuing from previous run). **Note**: this option will result in duplicate items being imported and is primarily intended to be used during non-production imports when testing out the configuration.|
|--continue \<boolean>|False|Continue execution upon a critical error|

**Note:** if the project defined in configuration does not exist, you´ll get a question if you want to create it. 

## Example

```
wi-import --token myToken --url https://dev.azure.com/myproject --config config.json --force
```
