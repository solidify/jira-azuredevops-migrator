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
|**source-project**|True|string|Short name of the project to migrate from.|
|**target-project**|True|string|Name of the project to migrate to.|
|**query**|True|string|Name of the JQL query to use for identifying work items to migrate.|
|**workspace**|True|string|Location where logs and export data are saved on disk.|
|**epic-link-field**|False|string|Jira name of epic link field. Default = "Epic Link". **Note:** requires customization per account and sometimes project|
|**sprint-field**|False|string|Jira name of sprint field. Default = "Sprint". **Note:** requires customization per account and sometimes project|
|**batch-size**|False|integer|Number of items to retrieve with one call. Default = 20.|
|**log-level**|False|string|Debug, Info, Warning, Error or Critical. Default = "Debug".|
|**attachment-folder**|True|string|Location to store attachments.|
|**user-mapping-file**|False|string|Name of user mapping file.|
|**base-area-path**|False|string|Area path. Default = "Migrated".|
|**base-iteration-path**|False|string|Iteration path. Default = "Migrated".|
|**ignore-failed-links**|False|boolean|Set to True if failed links are to be ignored. Default = False.|
|**process-template**|False|string|Process template in the target DevOps project. Supported values: Scrum, Agile or CMMI. Default = "Scrum".|
|**link-map**|True|json|List of **links** to map between Jira and Azure DevOps/TFS work item link types.|
|**type-map**|True|json|List of the work item **types** you want to migrate from Jira to Azure DevOps/TFS.|
|**field-map**|True|json|List of **fields** you want to migrate from a Jira item to a Azure DevOps/TFS work item.|

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
|**source-type**|False|string|Name of Jira field to get custom field id from. Default = "id".|
|**for**|False|string|Types of work items this field should be migrated to, i.e. Bug, Task, Product backlog item in a comma-delimiter list. Default = "All".|
|**not-for**|False|string|Negation of above, i.e this field is for a Bug only and nothing else.|
|**type**|False|string|Data type, i.e string, int, double. Default = string|
|**mapper**|False|string|Mapper function used for value translation.|
|**mapping**|False|json|List of **values** to map to and from in the migration.|

## Value properties
Name-value pairs of field values to map in the migration.

|Name|Required|Type|Description|
|---|---|---|---|
|source|False|string|Source value.|
|target|False|string|Target value.|

## Example configuration

```json
{
  "source-project": "SCRUM",
  "target-project": "Scrum-Demo-From-Jira",
  "query": "project = SCRUM ORDER BY Rank ASC",
  "workspace": "C:\\Temp\\JiraExport\\",
  "epic-link-field": "Epic Link",
  "sprint-field": "Sprint",
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
        "target": "System.Description"
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
        "target": "System.History"
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
      }
    ]
  }
}
```
