# Migration configuration

The migration configuration is the core of the migration, where all details about what to migrate and how data is mapped is defined.

Check out the Azure DevOps documention below for inspiration on which fields to map, their meaning and data translation.

* [Work item field index](https://docs.microsoft.com/en-us/azure/devops/boards/work-items/guidance/work-item-field)
* [Agile process](https://docs.microsoft.com/en-us/azure/devops/boards/work-items/guidance/agile-process)
* [Scrum process](https://docs.microsoft.com/en-us/azure/devops/boards/work-items/guidance/scrum-process)
* [CMMI process](https://docs.microsoft.com/en-us/azure/devops/boards/work-items/guidance/cmmi-process)

## Structure 

The migration configuration file is defined in a json file with the properties documented below.

## Properties

|Name|Required|Type|Description|
|---|---|---|---|
|**source-project**|True|string|Short name of the project to migrate from|
|**target-project**|True|string|Name of the project to migrate to|
|**query**|True|string|Name of the JQL query to use for identifying work items to migrate|
|**workspace**|True|string|Location where logs and export data are saved on disk|
|**epic-link-field**|False|string|Jira id of epic link field. **Note:** requires customization per account and sometimes project|
|**sprint-field**|False|string|Jira id of sprint field. **Note:** requires customization per account and sometimes project|
|**batch-size**|False|integer|Number of items to retrieve with one call|
|**log-level**|False|string|Debug, Info, Warning, Error or Critical|
|**attachment-folder**|False|string|Location to store attachments|
|**user-mapping-file**|False|string|Name of user mapping file|
|**base-area-path**|False|string|Area path|
|**base-iteration-path**|False|string|Iteration path|
|**ignore-failed-links**|False|boolean|Set to True if failed links are to be ignored|
|**process-template**|False|string|Process template in the target DevOps project. Supported values: Scrum, Agile or CMMI|
|**field-map**|True|json|List of **fields** you want to migrate from a Jira item to a Azure DevOps/TFS work item|

## Field properties
|Name|Required|Type|Description|
|---|---|---|---|
|**source**|True|string|Name of Jira source field|
|**target**|True|string|Name of Azure DevOps/TFS target field (reference name)|
|**for**|False|string|Types of work items this field should be migrated to, i.e. Bug, Task, Product backlog item in a comma-delimiter list. Default to All|
|**not-for**|False|string|Negation of above, i.e this field is for a Bug only and nothing else. Defaults to none|
|**type**|False|string|Data type, i.e string, int, double. Defaults to string|
|**process-template**|False|string|Process template this field is available for, i.e Scrum, Agile or CMMI |
|**mapper**|False|string|Mapper function user for value translation|

## Example configuration

```json
{
  // the short name of the Jira project to migrate from
  "source-project": "SCRUM",
  // the name of the Azure DevOps project to migrate to
  "target-project": "Jira-Import",
  // the name of the query to use for identifying work items to migrate
  // the query must be a flat query
  "query": "project = SCRUM ORDER BY Rank ASC",
  // where logs and export data are saved on disk
  "workspace": "C:\\Temp\\JiraMigration\\",
  // requires customization per account and sometimes project
  "epic-link-field": "customfield_10008",
  "sprint-field": "customfield_10007",
  // how many items to retrieve with one call
  "batch-size": 20,
  // Debug, Info, Warning, Error or Critical
  "log-level": "Debug",
  // where to store attachments
  "attachment-folder": "Attachments",
  // user mapping file
  "user-mapping-file": "users.txt",
  "base-area-path": "Migrated",
  "base-iteration-path": "Migrated",
  "ignore-failed-links": true,
  "process-template": "Scrum", 
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
        "target": "System.Description"
      },
      {
        "source": "priority",
        "target": "Microsoft.VSTS.Common.Priority",
        "mapper": "MapPriority"
      },
      {
        "source": "customfield_10007",
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
        "target": "System.History"
      },
      {
        "source": "status",
        "target": "System.State",
        "not-for": "Task",
        "mapper": "MapStateBugAndPBI"
      },
      {
        "source": "status",
        "target": "System.State",
        "for": "Task",
        "mapper": "MapStateTask"
      },
      {
        "source": "customfield_10004",
        "target": "Microsoft.VSTS.Scheduling.Effort",
        "for": "Epic,Feature,Product Backlog Item,Bug",
        "type": "double"
      },
      {
        "source": "remainingEstimate",
        "target": "Microsoft.VSTS.Scheduling.RemainingWork",
        "for": "Bug,Task",
        "type": "double"
      },
      {
        "source": "description",
        "target": "Microsoft.VSTS.TCM.ReproSteps",
        "for": "Bug"
      }
    ]
  }
}
```