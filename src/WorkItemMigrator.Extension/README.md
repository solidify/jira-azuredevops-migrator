The Jira to Azure DevOps work item migration tool lets you export data from Jira and import it as work items in Azure DevOps or Microsoft Team Foundation Server.

|Build|Release|
|---|---|
|[![Build status](https://dev.azure.com/solidify/Internal/_apis/build/status/Tools/DevOps.Migration.JiraVSTS)](https://dev.azure.com/solidify/Internal/_build/latest?definitionId=39)|[![Deployment status](https://vsrm.dev.azure.com/solidify/_apis/public/Release/badge/430a0fc1-6d24-414b-9bef-8afa19eb4b15/19/45)](https://dev.azure.com/solidify/Internal/_releases2?definitionId=19&view=all&_a=releases)|

# Features

This tool lets you migrate work item from Jira to Azure DevOps or Microsoft Team Foundation Server. The tool has two parts, first Jira issues are exported to files then we import the files to Azure DevOps/TFS. We believe this is a good approach to migration since it allows you to export the data to migrate and validate it before importing it. It also makes it easy to migrate in batches.

Some of the capabilities include:

- Export Jira issues from Jira queries
- Map users from Jira to users in Azure DevOps/TFS
- Migrate work item field data
- Migrate links and attachments
- Migrate history

# Getting the tools

* Download [the latest release from GitHub](https://github.com/solidify/jira-azuredevops-migrator/releases) and extract the files to your local workspace.

# Getting started

The tools are provided as-is but will require detailed understanding of how to migrate work items between different systems in order to do a successful migration. If you need support or help with migrations feel free to [contact the team at Solidify](mailto:info@solidify.se) for expert consulting.

* See the [migration process overview](https://github.com/solidify/jira-azuredevops-migrator/blob/master/docs/overview.md) for information on how to get started.
* Read the article [Jira to VSTS migration: migrating work items](https://solidify.se/jira-to-vsts-migration-work-items/) for more context of the process.

# Tested with

The Jira to Azure DevOps work item migration tool has been tested on the following configurations:

- Atlassian Jira Cloud
- Azure DevOps
- Team Foundation Server 2018 update 3
