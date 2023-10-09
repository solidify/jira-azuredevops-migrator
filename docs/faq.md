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

- To map a custom field by value we have to add a mapping in the configuration file.
- First, inspect the REST API response to find the **field key** for your custom field. This is usually something like **customfield_12345**.

Example:

```json
{
    "source": "customfield_12345",
    "target": "Custom.TargetField"
}
```

## 4. How to migrate custom fields having dropdown lists?

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

## 5. How to migrate correct user from Jira to Azure DevOps and assign to the new work items?

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

## 6. How to migrate the Work Log (Time Spent, Remaining Estimate fields)?

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

## 7. How to map datetime fields

Here is how we can map datetime fields like ResolvedDate:

```json
{
  "source": "resolutiondate",
  "type": "datetime",
  "target": "Microsoft.VSTS.Common.ResolvedDate"
}
```
