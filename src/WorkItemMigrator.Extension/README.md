The Jira to Azure DevOps work item migration tool lets you export data from Jira and import it as work items in Azure DevOps or Microsoft Team Foundation Server.

|Build|Quality|Release|
|---|---|---|
|[![Build Status](https://dev.azure.com/solidify/OSS/_apis/build/status/jira-azuredevops-migrator?branchName=master)](https://dev.azure.com/solidify/OSS/_build?definitionId=50)|[![](https://sonarcloud.io/api/project_badges/measure?project=jira-azuredevops-migrator&metric=alert_status)](https://sonarcloud.io/dashboard?id=jira-azuredevops-migrator)|[![Deployment status](https://vsrm.dev.azure.com/solidify/_apis/public/Release/badge/9d04c453-c16d-4cd5-aadd-4162a63d5df5/4/20)](https://dev.azure.com/solidify/OSS/_release?definitionId=4)|

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

The tools are provided as-is and will require detailed understanding of how to migrate work items between different systems in order to do a successful migration. If you need support or help with migrations feel free to [contact the team at Solidify](mailto:support.jira-migrator@solidify.dev) for expert consulting services.

* See the [migration process overview](https://github.com/solidify/jira-azuredevops-migrator/blob/master/docs/overview.md) for information on how to get started.
* Read the article [Jira to VSTS migration: migrating work items](https://solidify.se/blog/jira-to-vsts-migration-work-items) for more context of the process.
* Read the article [Jira to Azure DevOps (VSTS or TFS) migration](https://solidify.se/blog/jira-azure-devops-migration) for a complete step-by-step walkthrough on a migration.

# Jira Azure DevOps Migrator PRO

The **Jira Azure DevOps Migrator PRO offering** from Solidify offers more features and utilities to further increase your migration capabilities and streamline the migration workflow. [Contact us for more information](mailto:support.jira-migrator@solidify.dev)

## Features

**Jira Azure DevOps Migrator PRO** contains all the features in the **Community Edition**, plus the following additional functionality:

- Priority support
- Composite field mapper (consolidate multiple Jira fields into a single ADO field)
- Migrate **Releases** and the **fixes version** field
- Migrate **Remote Links** (Web links) to Work Item hyperlinks.
- Correct any **Embedded Links to Jira Issues** in text fields such as Description, Repro Steps and comments, so that they point to the correct Work Item in Azure DevOps.
- Select any property for **object**- and **array**-type fields for mapping. This allows for:
  - More possibilities when mapping the **fixes version** and **components** fields.
  - Support for mapping **custom user picker** fields.
- Utilities for automating user mapping between Jira and Azure DevOps
- Utilities for automatically generating the Jira Azure DevOps Migrator configuration file, thus enabling you to get started migrating faster
- Utilities for viewing the Jira workflow and assisting with field and state mapping

# Supported versions of ADO/Jira

The Jira to Azure DevOps work item migration tool is officially supported on the following platforms:

- Atlassian Jira Cloud
- Atlassian Jira Server
  - 7.x
  - 8.x
  - 9.x
- Azure DevOps Services
- Azure DevOps Server
  - 2022
  - 2020
  - 2019
- Team Foundation Server 2018 update 3
