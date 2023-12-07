# FAQ - Frequently Asked Questions

## 1. Convert Jira formatted descriptions and comments on migration

- With our latest release (2.3.1) we have introduced a new mapper called "MapRendered" that should be used when mapping fields to get the Html rendered value from Jira.

Example:

```json
{
    "source": "description",
    "target": "Microsoft.VSTS.TCM.ReproSteps",
    "mapper":"MapRendered"
}
```

## 2. Why I am getting Unauthorized exception when running the export?

- Ensure that your Jira credentials and Jira URL are correct.
- Ensure that your `jira-export` command and all the flags are correct. See: <https://github.com/solidify/jira-azuredevops-migrator/blob/doc/update-usage-examples/docs/jira-export.md>
- Try different combinations of your jira user/api credentials. The functionality here could depend on wether you are using Jira Cloud or Jira Server, as well as wether you have set your user's email as public in the user profile in Jira Cloud, and jira might not be accepting certain credentials. Try all combinations of the following:
  - username: **email**
  - username: **Jira Username**
  - username: **accountId** (navigate to your user profile and find the accountId in the URL, for example: <https://solidifydemo.atlassian.net/jira/people/>**6038bfcc25b84ea0696240d4**
  - password: **user password** (same as login)
  - password: **API token**

Another problem could be that you have characters in your `--password` parameter that is reserved by the terminal, e.g. **dollar sign** ($) in Powershell. A potential solution sometimes is to escape any dollar sign characters. So make sure that your `--password` parameter is properly escaped, depending on what terminal you are using. Example for Powershell: `$` becomes `$. Otherwise you can always try a different terminal like CMD or bash.

If you are still not able to authenticate. Try and run the tool as another user. Also make sure to try as a user with admin privileges in your Jira organization.

## 3. How to map custom field?

- To map a custom field by value we have to add a mapping in the configuration file, using the custom field name:

```json
    {
        "source": "Custom Field Name Jira",
        "source-type": "name",
        "target": "Custom.TargetField"
    },
```

- Alternatively, we can map the field key instead of the name. Inspect the REST API response to find the **field key** for your custom field. This is usually something like **customfield_12345**.

Example:

```json
{
    "source": "customfield_12345",
    "target": "Custom.TargetField"
}
```

### (Troubleshooting) My custom field is not migrated correctly/not migrated at all.

If your custom field is not imported correctly into Azure DevOps, please go through the following checklist and ensure that every step has been followed:

1. Ensure that the field is created in the correct Azure DevOps process model, and that the field is existing on the correct work item type.
2. Ensure that the `target` of your field mapping is set to the **Field reference name** of the ADO field, not the **Field name** (Observe that these two are different!!!)

    For example, if the **field name** is `MyField`, the **field reference name** is usually something like `Custom.MyField` (for ADO Services) or `MyCompany.MyField` (for ADO Server). Spaces are not allowed in the **field reference name**.

    Here is a reference sheet with all of the default fields: https://learn.microsoft.com/en-us/azure/devops/boards/work-items/guidance/work-item-field?view=azure-devops (click each field to open up the documentation page and view the field reference name).

### (Troubleshooting) I receive errors like: **VS403691: Update to work item 165 had two or more updates for field with reference name 'Custom.XXX'. A field cannot be updated more than once in the same update."**

This error is usually indicative of incorrect configuration on the user's side. Please follow the checklist [here](https://github.com/solidify/jira-azuredevops-migrator/blob/master/docs/faq.md#troubleshooting-my-custom-field-is-not-migrated-correctlynot-migrated-at-all) (the section above this one, in the same document) to ensure that you do not have any issues with your `config.json` file.

## 4. Guideline for migrating multiple projects

### Scenario 1: Single project

When migrating a single project, you may have issues with links to other issues that are in other projects or otherwise not captured by the JQL query.

You can include such linked issues (all parents, epic links and sub items) by setting the following property in the configuration file:

```json
  "download-options": 7
```

See <https://github.com/solidify/jira-azuredevops-migrator/blob/master/docs/config.md#download-options> for more information on the `download-options` property.

### Scenario 2: Multiple projects

When migrating multiple project, one after another (or otherwise running several migrations with different queries in a serial fashion), you may get duplicate issues if you have enabled the *include-linked-issues-not-captured-by-query* flag.

The recommendation is thus to turn off all linked issues (parents, epic links and sub items) by setting the following property in the configuration file:

```json
  "download-options": 0
```

See <https://github.com/solidify/jira-azuredevops-migrator/blob/master/docs/config.md#download-options> for more information on the `download-options` property.

When running multiple migrations, one after another, we recommend following the below guidelines:

- Use one separate `workspace` folder per migration.
- For every completed migration, locate the `itemsJournal.txt` file inside your `workspace` folder. Copy this file to the workspace folder of the next migration. Then proceed with the net migration. This will ensure that you do not get duplicates, and any cross-project or cross-query links will be intact.

## 5. How to migrate custom fields having dropdown lists?

- To map a custom field which is an dropdown list you can use MapArray mapper to get in a better way.
Also take a look at the other possible [Mappers](config.md#mappers) to use.

Example:

```json
{
    "source": "UserPicker",
    "source-type": "name",
    "target": "Custom.TextField",
    "for": "Product Backlog Item",
    "mapper": "MapArray"
}
```

## 6. How to migrate correct user from Jira to Azure DevOps and assign to the new work items?

- User mapping differs between Jira Cloud and Jira Server. To migrate users and assign the new work items in Azure DevOps to the same user as the original task had in Jira, we need to add a text file in the root that would look something like this:

    ```txt
    Jira.User1@some.domain=AzureDevOps.User1@some.domain
    Jira.User2@some.domain=AzureDevOps.User2@some.domain
    Jira.User3@some.domain=AzureDevOps.User3@some.domain
    ```

- When using Jira Cloud then firstly make sure in the config the '"using-jira-cloud": true' is set. The user mapping file should have accountId/email value pairs. To use email value pairs the users email should be set to public in the user profile in Jira Cloud, otherwise the tool cant get the email and will use accountId instead for mapping.

- It can happen that the **JiraExporter** cannot find you users' email addresses. This will happen if e.g. your user has chosen not to make their email address public. You will then receive the following warning when running the **jira-expoprt.exe**:

    ```
    [W][01:57:30] Email is not public for user '630ddc7d316bbc88c1234e3b' in Jira, using usernameOrAccountId '630ddc7d316bbc88c1234e3b' for mapping.
    ```

    If you receive such a warning, you will need to map the users' IDs instead, just like you would with the emails. You will need to include the following line in your `users.txt`:

    ```
    630ddc7d316bbc88c1234e3b=AzureDevOps.User@some.domain
    ```

    The correct format of the `users.txt`-file would then be:

    ```txt
    JiraAccountId1=AzureDevOps.User1@some.domain
    JiraAccountId2=AzureDevOps.User2@some.domain
    JiraAccountId3=AzureDevOps.User3@some.domain
    ```

- When using Jira Server then firstly make sure in the config the ' "using-jira-cloud": false' is set. The mapping should look like the example below:

    ```txt
    Jira.User1@some.domain=AzureDevOps.User1@some.domain
    Jira.User2@some.domain=AzureDevOps.User2@some.domain
    Jira.User3@some.domain=AzureDevOps.User3@some.domain
    ```

## 7. How to migrate the Work Log (Time Spent, Remaining Estimate fields)?

You can migrate the logged and remaining time using the following field mappings.

The history of the **logged time** and **remaining time** will be preserved on each revision, and thus the history of these fields will be migrated just like any other field.

```json
{
  "source": "timespent$Rendered",
  "target": "Custom.TimeSpentSecondsRendered"
},
{
  "source": "timespent",
  "target": "Custom.TimeSpentSeconds"
},
{
  "source": "timeestimate$Rendered",
  "target": "Custom.RemainingEstimateSecondsRendered"
},
{
  "source": "timeestimate",
  "target": "Custom.RemainingEstimateSeconds"
}
```

## 8. How to map custom userpicker fields

Here is how we have successfully mapped userpicker fields in the past. `source` should be the field name:

```json
{
    "source": "Userpicker Field Name Jira",
    "target": "Custom.CustomUserPicker",
    "source-type": "name",
    "mapper": "MapUser"
},
```

## 9. How to map datetime fields

Here is how we can map datetime fields like ResolvedDate:

```json
{
  "source": "resolutiondate",
  "type": "datetime",
  "target": "Microsoft.VSTS.Common.ResolvedDate"
}
```

## 10. How to migrate an issue fields to a comment

Through some manual intervention, we can migrate every historical value of an **issue field** to a **Work Item Comments**. Simply do the following:

1. Map each of the desired fields to a unique token, e.g.:
  ```json
  {
    "source": "customfield_10112",
    "target": "5397700c-5bc3-4efe-b1e9-d626929b89ca"
  },
  {
    "source": "customfield_10111",
    "target": "e0cd3eb0-d8b7-4e62-ba35-c24d06d7f667"
  },
  ```
1. Run `JiraExport` as usual.
1. Open up the `workspace` folder in an IDE like Visual Studio Code and do a search-replace across all contents in the whole `workspace` folder:
1. Replace each unique token with `System.History`:
   - `5397700c-5bc3-4efe-b1e9-d626929b89ca` > `System.History`
   - `e0cd3eb0-d8b7-4e62-ba35-c24d06d7f667` > `System.History`
1. Run `WiImport` as usual.

## 11. How to omit the Jira issue ID/key in the work item title

By default, the field mapping for `System.Title` will be set up so that the title is prefixed with the Issue key. This can be prevented by omitting the **MapTitle mapper** from the field map in the configuration:

```json
  {
    "source": "summary",
    "target": "System.Title"
  }
```

Instead of the default:

```json
  {
    "source": "summary",
    "target": "System.Title",
    "mapper": "MapTitle"
  }
```
