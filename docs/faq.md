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

## 2. Why I am getting Unauthorized exception when running export?

- It might be that you are using your email as a username, try to use your username instead of an email address.
- using Jira Cloud - it might be that you need to to use the API token as a password.

## 3. How to map custom field by name?

- To map a custom field by name we have to add a mapping in the configuration file.

Example:

```json
{
    "source": "CustomFieldName",
    "source-type": "name",
    "target": "Microsoft.VSTS.TCM.ReproSteps"
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

- When using Jira Cloud then firstly make sure in the config the '"using-jira-cloud": true' is set. The mapping file the should have accountId/email value pairs. To use email value pairs the users email should be set to public in the user profile in Jira Cloud, otherwise the tool cant get the email and will use accountId instead for mapping.

    ```txt
    Jira.User1@some.domain=AzureDevOps.User1@some.domain
    Jira.User2@some.domain=AzureDevOps.User2@some.domain
    Jira.User3@some.domain=AzureDevOps.User3@some.domain
    ```

    or

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
