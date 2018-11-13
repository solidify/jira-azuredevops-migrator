The Jira to Azure DevOps work item migration tool lets you export data from Jira and import it as work items in Azure DevOps or Microsoft Team Foundation Server.

|Build|Quality|Release|
|---|---|---|
|[![Build Status](https://solidify.visualstudio.com/OSS/_apis/build/status/jira-azuredevops-migrator)](https://solidify.visualstudio.com/OSS/_build/latest?definitionId=50)|[![](https://sonarcloud.io/api/project_badges/measure?project=jira-azuredevops-migrator&metric=alert_status)](https://sonarcloud.io/dashboard?id=jira-azuredevops-migrator)|[![Deployment status](https://solidify.vsrm.visualstudio.com/_apis/public/Release/badge/9d04c453-c16d-4cd5-aadd-4162a63d5df5/4/12)](https://solidify.visualstudio.com/OSS/_release?definitionId=4)|

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
