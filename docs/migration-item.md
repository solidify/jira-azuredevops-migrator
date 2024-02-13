# Migration item file

This document describes the structure of the migration item file.

## Structure

## Example item

```json
{
    "Type": "Bug",
    "OriginId": "SCRUM-17",
    "WiId": -1,
    "Revisions": [
        {
            "Author": "some.user@azuredevops.domain",
            "Time": "2018-01-29T03:46:18.5+01:00",
            "Index": 0,
            "Fields": [
                {
                    "ReferenceName": "System.Title",
                    "Value": "[SCRUM-17] Instructions for deleting this sample board and project are in the description for this issue >> Click the \"SCRUM-17\" link and read the description tab of the detail view for more"
                },
                {
                    "ReferenceName": "System.AssignedTo",
                    "Value": "some.user@azuredevops.domain"
                },
                {
                    "ReferenceName": "System.Description",
                    "Value": "*To delete this Sample Project _(must be performed by a user with Administration rights)_* \n- Open the administration interface to the projects page by using the keyboard shortcut 'g' then 'g' and typing 'Projects' in to the search dialog\n- Select the \"Delete\" link for the \"Scrum-Demo\" project\n\n*To delete the Sample Project workflow and workflow scheme _(must be performed by a user with Administration rights)_* \n- Open the administration interface to the workflow schemes page by using the keyboard shortcut 'g' then 'g' and typing 'Workflow Schemes' in to the search dialog\n- Select the \"Delete\" link for the \"SCRUM: Software Simplified Workflow Scheme\" workflow scheme\n- Go to the workflows page by using the keyboard shortcut 'g' then 'g' and typing 'Workflows' in to the search dialog(_OnDemand users should select the second match for Workflows_)\n- Expand the \"Inactive\" section\n- Select the \"Delete\" link for the \"Software Simplified Workflow  for Project SCRUM\" workflow\n\n*To delete this Board _(must be performed by the owner of this Board or an Administrator)_*\n- Click the \"Tools\" cog at the top right of this board\n- Select \"Delete\""
                },
                {
                    "ReferenceName": "Microsoft.VSTS.Common.Priority",
                    "Value": 3
                },
                {
                    "ReferenceName": "System.State",
                    "Value": "New"
                },
                {
                    "ReferenceName": "Microsoft.VSTS.TCM.ReproSteps",
                    "Value": "<style>div {\r\n    display: block;\r\n}\r\n\r\ntable.confluenceTable {\r\n    border-collapse: collapse;\r\n    margin: 5px 0 5px 2px;\r\n    width: auto;\r\n}\r\n\r\ntable {\r\n    display: table;\r\n    border-collapse: separate;\r\n    border-spacing: 2px;\r\n    border-color: grey;\r\n}\r\n\r\ntbody {\r\n    display: table-row-group;\r\n    vertical-align: middle;\r\n    border-color: inherit;\r\n}\r\n\r\ntr {\r\n    display: table-row;\r\n    vertical-align: inherit;\r\n    border-color: inherit;\r\n}\r\n\r\nth.confluenceTh {\r\n    border: 1px solid #ccc;\r\n    background: #f5f5f5;\r\n    padding: 3px 4px;\r\n    text-align: center;\r\n}\r\n\r\nth {\r\n    font-weight: bold;\r\n    text-align: -internal-center;\r\n}\r\n\r\ntd, th {\r\n    display: table-cell;\r\n    vertical-align: inherit;\r\n}\r\n\r\n    td.confluenceTd {\r\n        border: 1px solid #ccc;\r\n        padding: 3px 4px;\r\n    }\r\n\r\ndfn, cite {\r\n    font-style: italic;\r\n}\r\n\r\n    cite:before {\r\n        content: \"\\2014 \\2009\";\r\n    }\r\n</style><p><b>To delete this Sample Project <em>(must be performed by a user with Administration rights)</em></b> </p>\n<ul class=\"alternate\" type=\"square\">\n\t<li>Open the administration interface to the projects page by using the keyboard shortcut 'g' then 'g' and typing 'Projects' in to the search dialog</li>\n\t<li>Select the \"Delete\" link for the \"Scrum-Demo\" project</li>\n</ul>\n\n\n<p><b>To delete the Sample Project workflow and workflow scheme <em>(must be performed by a user with Administration rights)</em></b> </p>\n<ul class=\"alternate\" type=\"square\">\n\t<li>Open the administration interface to the workflow schemes page by using the keyboard shortcut 'g' then 'g' and typing 'Workflow Schemes' in to the search dialog</li>\n\t<li>Select the \"Delete\" link for the \"SCRUM: Software Simplified Workflow Scheme\" workflow scheme</li>\n\t<li>Go to the workflows page by using the keyboard shortcut 'g' then 'g' and typing 'Workflows' in to the search dialog(<em>OnDemand users should select the second match for Workflows</em>)</li>\n\t<li>Expand the \"Inactive\" section</li>\n\t<li>Select the \"Delete\" link for the \"Software Simplified Workflow  for Project SCRUM\" workflow</li>\n</ul>\n\n\n<p><b>To delete this Board <em>(must be performed by the owner of this Board or an Administrator)</em></b></p>\n<ul class=\"alternate\" type=\"square\">\n\t<li>Click the \"Tools\" cog at the top right of this board</li>\n\t<li>Select \"Delete\"</li>\n</ul>\n"
                }
            ],
            "Links": [],
            "Attachments": [],
            "AttachmentReferences": true
        },
        {
            "Author": "some.user@azuredevops.domain",
            "Time": "2018-01-29T14:30:18.5+01:00",
            "Index": 1,
            "Fields": [
                {
                    "ReferenceName": "System.State",
                    "Value": "Committed"
                }
            ],
            "Links": [],
            "Attachments": [],
            "AttachmentReferences": false
        },
        {
            "Author": "some.user@azuredevops.domain",
            "Time": "2018-02-01T20:22:18.5+01:00",
            "Index": 2,
            "Fields": [
                {
                    "ReferenceName": "System.State",
                    "Value": "Done"
                }
            ],
            "Links": [],
            "Attachments": [],
            "AttachmentReferences": false
        },
        {
            "Author": "some.user@azuredevops.domain",
            "Time": "2018-02-01T20:22:18.5+01:00",
            "Index": 3,
            "Fields": [
                {
                    "ReferenceName": "System.History",
                    "Value": "Joined Sample Sprint 2 7 days 9 hours 10 minutes ago"
                }
            ],
            "Links": [],
            "Attachments": [],
            "AttachmentReferences": false
        },
        {
            "Author": "some.user@azuredevops.domain",
            "Time": "2018-02-01T20:22:18.5+01:00",
            "Index": 4,
            "Fields": [
                {
                    "ReferenceName": "System.History",
                    "Value": "To Do to In Progress 6 days 22 hours 26 minutes ago\r\nIn Progress to Done 3 days 16 hours 34 minutes ago"
                }
            ],
            "Links": [],
            "Attachments": [],
            "AttachmentReferences": false
        }
    ]
}
```
