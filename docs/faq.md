## FAQ - Frequently Asked Questions
1. Convert Jira formatted descriptions and comments on migration
- With our latest release (2.3.1) we have introduced a new mapper called "MapRendered" that should be used when mapping fields to get the Html rendered value from Jira. 

Example:
`{`
    `"source": "description",`
    `"target": "Microsoft.VSTS.TCM.ReproSteps",`
    `"mapper":"MapRendered"`
`}`

2. Why I am getting Unauthorized exception when running export?
-   It might be that you are using your email as a username, try to use your username instead of an email address.
- using Jira Cloud   - it might be that you need to to use the API token as a password.

3. How to map custom field by name?
 - To map a custom field by name we have to add a mapping in the configuration file.

 Example: 
`{
    "source": "CustomFieldName",
    "source-type": "name",
    "target": "Microsoft.VSTS.TCM.ReproSteps"
}`

4. How to migrate custom fields having dropdownlists?
- To map a custom field which is an dropdown list you can use MapArray mapper to get in a better way.
Also take a look at the other possible [Mappers](config.md#mappers) to use. 

Example: 
` {
        "source": "UserPicker",
        "source-type": "name",
        "target": "Custom.TextField",
        "for": "Product Backlog Item",
        "mapper": "MapArray"
    }
`

5. How to migrate correct user from Jira to Azure DevOps and assign to the new work items ?
- User mapping  differes between Jira Cloud and Jira Server. To migrate users and assign the new work items in Azure DevOps to the same user as the original task had in Jira, we need to add a text file in the root that would look something like this:

 - When using Jira Cloud then firstly make sure in the config the '"using-jira-cloud": true' is set. The mapping file the should have accountId/email value pairs. To use email value pairs the users email should be set to public in the user profile in Jira Cloud
 otherwise the tool cant get the email and will use accountId instead for mapping.

        Some.JiraUser@domain.com=Some.AzureDevOpsUser@domain.com or JiraAccountId=Some.AzureDevOpsUser@domain.com

 - When using Jira Server then firstly make sure in the config the ' "using-jira-cloud": false' is set. The mapping should look like the example below:

    Some.JiraUser@domain.com=Some.AzureDevOpsUser@domain.com

