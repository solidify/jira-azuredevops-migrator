# Migration configuration

The migration configuration is the core of the migration, where all details about what to migrate and how data is mapped is defined.

Check out the Azure DevOps documentation below for inspiration on which fields to map, their meaning and data translation.

* [Work item field index](https://docs.microsoft.com/en-us/azure/devops/boards/work-items/guidance/work-item-field)
* [Agile process](https://docs.microsoft.com/en-us/azure/devops/boards/work-items/guidance/agile-process)
* [Scrum process](https://docs.microsoft.com/en-us/azure/devops/boards/work-items/guidance/scrum-process)
* [CMMI process](https://docs.microsoft.com/en-us/azure/devops/boards/work-items/guidance/cmmi-process)

## Structure

The migration configuration file is defined in a json file with the properties documented below.

## Properties

|Name|Required|Type|Description|
|---|---|---|---|
|**source-project**|True|string|Short name of the project to migrate from.|
|**target-project**|True|string|Name of the project to migrate to.|
|**query**|True|string|Name of the JQL query to use for identifying work items to migrate.|
|**using-jira-cloud**|False|boolean|Set to False if connected to Jira Server instance, by default it is True|
|**workspace**|True|string|Location where logs and export data are saved on disk.|
|**epic-link-field**|False|string|Jira name of epic link field. Default = "Epic Link". **Note:** requires customization per account and sometimes project|
|**sprint-field**|False|string|Jira name of sprint field. Default = "Sprint". **Note:** requires customization per account and sometimes project|
|**batch-size**|False|integer|Number of items to retrieve with one call. Default = 20.|
|**download-options**|False|integer|Type of related issues to migrate, see **Download options** below|
|**log-level**|False|string|Debug, Info, Warning, Error or Critical. Default = "Debug".|
|**attachment-folder**|True|string|Location to store attachments.|
|**user-mapping-file**|False|string|Name of user mapping file. If no specific path is set the program expects it to be located in the "workspace" folder.|
|**base-area-path**|False|string|The root area path under which all migrated work items will be placed. Default is empty.|
|**base-iteration-path**|False|string|The root iteration path for the migrated work items. Default is empty.|
|**ignore-failed-links**|False|boolean|Set to True if failed links are to be ignored. Default = False.|
|**include-link-comments**|False|boolean|Set to True to get a verbose comment on the work item for every work item link created. Default = True.|
|**include-jira-css-styles**|True|boolean|Set to True to generate and include confluence CSS Stylesheets for description, repro steps and comments. Default = True.|
|**ignore-empty-revisions**|False|boolean|Set to True to ignore importing empty revisions. Empty revisions will be created if you have historical revisions where none of the changed fields or links have been mapped. This may indicate that you have unmapped data, which will not be migrated. Default = False.|
|**suppress-notifications**|False|boolean|Set to True to suppress all notifications in Azure DevOps about created and updated Work Items. Default = False.|
|**include-development-links**|False|boolean|Set to True to migrated commit links from Jira to Azure DevOps. You will also need to fill out the **repository-map** property. Default = False.|
|**sleep-time-between-revision-import-milliseconds**|False|integer|How many milliseconds to sleep between each revision import. Use this if throttling is an issue for ADO Services. Default = 0 (no sleep).|
|**changeddate-bump-ms**|False|integer|How many milliseconds to buffer each subsequent revision if there is a negative revision timestamp offset. Increase this if you get a lot of VS402625 warning messages during the import. Default = 2 (ms).|
|**process-template**|False|string|Process template in the target DevOps project. Supported values: Scrum, Agile or CMMI. Default = "Scrum".|
|**link-map**|True|json|List of **links** to map between Jira and Azure DevOps/TFS work item link types.|
|**type-map**|True|json|List of the work item **types** you want to migrate from Jira to Azure DevOps/TFS.|
|**field-map**|True|json|List of **fields** you want to migrate from a Jira item to a Azure DevOps/TFS work item.|
|**repository-map**|True|json|List of **repositories** you want to map from a bitbucket Azure DevOps/TFS. This enables migration of commit links, but only if the **include-development-links** property has been set to **true** and the git repositories have already been migrated from BitBucket to Azure DevOps.|

## Download options

This option allows the tool to download related issues to cover cases where these are not included in the section query (like a parent issue).

Default value: 7 (=all)

|Option|Value|
|---|---|
|None|0|
|IncludeParentEpics|1|
|IncludeParents|2|
|IncludeSubItems|4|

## Link properties

Name-value pairs of work item link types to map in the migration.

|Name|Required|Type|Description|
|---|---|---|---|
|source|True|string|Source Jira link type.|
|target|True|string|Target Azure DevOps/TFS link type.|

## Type properties

Name-value pairs of work item types to map in the migration.

|Name|Required|Type|Description|
|---|---|---|---|
|source|True|string|Source Jira work item type.|
|target|True|string|Target Azure DevOps/TFS work item type.|

## Field properties

|Name|Required|Type|Description|
|---|---|---|---|
|**source**|True|string|Name of Jira source field.|
|**target**|True|string|Name of Azure DevOps/TFS target field (reference name).|
|**source-type**|False|string|Name of Jira field to get custom field id from. Default = "id". When using Jira Server do not use this when mapping to custom User Picker field.|
|**for**|False|string|Types of work items this field should be migrated to, i.e. Bug, Task, Product backlog item in a comma-delimiter list. Default = "All".  When adding for ensure that a TypeMap.target is specified, specifying a TypeMap.source will cause fields to not be merged.|
|**not-for**|False|string|Negation of above, i.e this field is for a Bug only and nothing else.  When adding for ensure that a TypeMap.target is specified, specifying a TypeMap.source will cause fields to not be merged.|
|**type**|False|string|Data type, i.e string, int, double. Default = string|
|**mapper**|False|string|Mapper function used for value translation. See the **Mappers** section below for a quick summary of the available mappers.|
|**mapping**|False|json|List of **key value pairs** to map to and from in the migration. Use this instead of the **mapper** property if you need a simple key-value mapping.|

## Value properties

Name-value pairs of field values to map in the migration.

|Name|Required|Type|Description|
|---|---|---|---|
|source|False|string|Source value.|
|target|False|string|Target value.|

## Repository properties

Name-value pairs of git repositories to map in the migration.

|Name|Required|Type|Description|
|---|---|---|---|
|source|False|string|Repository name in BitBucket.|
|target|False|string|Repository name in Azure DevOps/TFS.|

## Mappers

Mappers are functions used byt he **Jira Exporter** for transforming the data in the Jira issue fields. The table below is as a summary and explanation of the different mappers available.

**Note**: the source code for the mapping logic is here: <https://github.com/solidify/jira-azuredevops-migrator/blob/master/src/WorkItemMigrator/JiraExport/JiraMapper.cs>

|Name|Description|
|---|---|
|MapTitle|Maps summary on the format [id] summary|
|MapTitleWithoutKey|Maps summary field without [id]|
|MapUser|Maps users based on email or name by lookup in the users.txt if specified, this applies only for Jira Server. When using Jira Cloud mapping can be done email if email is allowed to be displayed on the user profile or by accountId|
|MapSprint|Maps a sprint by matching the Azure DevOps iteration tree|
|MapTags|Maps tags by replacing space with semi-colon|
|MapArray|Maps an array by replacing comma with semi-colon|
|MapRemainingWork|Maps and converts a Jira time to hours|
|MapRendered|Maps field to rendered html format value|
|MapLexoRank|Maps and converts a Jira LexoRank to decimal. When mapping this type of field, ensure the correct Jira custom field is used and mapped to the relevant Azure DevOps prioritization field (see: <https://learn.microsoft.com/en-us/azure/devops/boards/queries/planning-ranking-priorities?view=azure-devops#fields-used-to-plan-and-prioritize-work>)|
|(default)|Simply copies source to target|

## Example configuration

```json
{
  "source-project": "SCRUM",
  "target-project": "Scrum-Demo-From-Jira",
  "query": "project = \"SCRUM\" ORDER BY Rank ASC",
  "using-jira-cloud": true,
  "workspace": "C:\\Temp\\JiraExport\\",
  "epic-link-field": "Epic Link",
  "sprint-field": "Sprint",
  "download-options": 7,
  "batch-size": 20,
  "log-level": "Debug",
  "attachment-folder": "Attachments",
  "user-mapping-file": "users.txt",
  "base-area-path": "Migrated",
  "base-iteration-path": "Migrated",
  "ignore-failed-links": true,
  "process-template": "Scrum",
  "link-map": {
    "link": [
      {
        "source": "Epic",
        "target": "System.LinkTypes.Hierarchy-Reverse"
      },
      {
        "source": "Parent",
        "target": "System.LinkTypes.Hierarchy-Reverse"
      },
      {
        "source": "Relates",
        "target": "System.LinkTypes.Related"
      },
      {
        "source": "Duplicate",
        "target": "System.LinkTypes.Duplicate-Forward"
      }
    ]
  },
  "type-map":{
    "type": [
      {
        "source": "Feature",
        "target": "Feature"
      },
      {
        "source": "Epic",
        "target": "Epic"
      },
      {
        "source": "Story",
        "target": "Product Backlog Item"
      },
      {
        "source": "Bug",
        "target": "Bug"
      },
      {
        "source": "Task",
        "target": "Product Backlog Item"
      },
      {
        "source": "Sub-task",
        "target": "Task"
      }
    ]
  },
  "field-map": {
    "field": [
      {
        "source": "summary",
        "target": "System.Title",
        "mapper": "MapTitle"
      },
      {
        "source": "assignee",
        "target": "System.AssignedTo",
        "mapper": "MapUser"
      },
      {
        "source": "description",
        "target": "System.Description",
        "mapper":"MapRendered"
      },
      {
        "source": "priority",
        "target": "Microsoft.VSTS.Common.Priority",
        "mapping": {
          "values": [
            {
              "source": "Blocker",
              "target": "1"
            },
            {
              "source": "Critical",
              "target": "1"
            },
            {
              "source": "Highest",
              "target": "1"
            },
            {
              "source": "Major",
              "target": "2"
            },
            {
              "source": "High",
              "target": "2"
            },
            {
              "source": "Medium",
              "target": "3"
            },
            {
              "source": "Low",
              "target": "3"
            },
            {
              "source": "Lowest",
              "target": "4"
            },
            {
              "source": "Minor",
              "target": "4"
            },
            {
              "source": "Trivial",
              "target": "4"
            }
          ]
        }
      },
      {
        "source": "Sprint",
        "source-type": "name",
        "target": "System.IterationPath",
        "mapper": "MapSprint"
      },
      {
        "source": "labels",
        "target": "System.Tags",
        "mapper": "MapTags"
      },
      {
        "source": "comment",
        "target": "System.History",
        "mapper":"MapRendered"
      },
      {
        "source": "status",
        "target": "System.State",
        "for": "Task",
        "mapping": {
          "values": [
            {
              "source": "To Do",
              "target": "To Do"
            },
            {
              "source": "Done",
              "target": "Done"
            },
            {
              "source": "In Progress",
              "target": "In Progress"
            }
          ]
        }
      },
      {
        "source": "status",
        "target": "System.State",
        "for": "Bug,Product Backlog Item",
        "mapping": {
          "values": [
            {
              "source": "To Do",
              "target": "New"
            },
            {
              "source": "Done",
              "target": "Done"
            },
            {
              "source": "In Progress",
              "target": "Committed"
            }
          ]
        }
      },
      {
        "source": "status",
        "target": "System.State",
        "for": "Epic,Feature",
        "mapping": {
          "values": [
            {
              "source": "To Do",
              "target": "New"
            },
            {
              "source": "Done",
              "target": "Done"
            },
            {
              "source": "In Progress",
              "target": "In Progress"
            }
          ]
        }
      },
      {
        "source": "Story Points",
        "source-type": "name",
        "target": "Microsoft.VSTS.Scheduling.Effort",
        "not-for": "Task"
      },
      {
        "source": "remainingEstimate",
        "target": "Microsoft.VSTS.Scheduling.RemainingWork",
        "for": "Bug,Task"
      },
      {
        "source": "description",
        "target": "Microsoft.VSTS.TCM.ReproSteps",
        "for": "Bug"
      },
      {
        "source": "customfield_10015",
        "target": "Microsoft.VSTS.Common.BacklogPriority",
        "mapper": "MapLexoRank"
      }
    ]
  }
}
```
