from distutils.log import error
import sys
from requests.auth import HTTPBasicAuth
import requests
import base64
from dateutil import parser as dateparser

##########################
###### ADO REST API ######
##########################

### Wiql - Query By Wiql
# https://docs.microsoft.com/en-us/rest/api/azure/devops/wit/wiql/query-by-wiql
# Params:
# - project_name: The friendly name or guid of the project
# - team_name: The guid or friendly name of the team (optional)
# - query: The wiql query.
def list_workitems_by_wiql(
    PAT: str, organization_url: str, project_name: str, query: str = ""
):
    headers = {
        "Authorization": create_auth_header_ado(PAT),
        "Content-Type": "application/json",
    }
    body = '{"query": "' + query + '"}'

    uri_api = "{0}/{1}/_apis/wit/wiql?api-version=6.0".format(
        organization_url, project_name
    )
    response = requests.post(url=uri_api, headers=headers, data=body)
    r = None
    try:
        r = response.json()
    except:
        print("Http error while sending to the enpoint: " + uri_api)
        print(response)
        print("body:" + body)
    return r


### Work Items - Get
# https://docs.microsoft.com/en-us/rest/api/azure/devops/wit/work-items/get
# Params:
# - project_name: The friendly name or guid of the project
# - id: ID of the work item
def get_workitem(PAT: str, organization_url: str, project_name: str, id: str):
    headers = {
        "Authorization": create_auth_header_ado(PAT),
        "Content-Type": "application/json",
    }
    uri_api = "{0}/{1}/_apis/wit/workitems/{2}?$expand=All&api-version=6.0".format(
        organization_url, project_name, id
    )
    response = requests.get(url=uri_api, headers=headers)
    return response.json()


### Work Items - Delete
# https://docs.microsoft.com/en-us/rest/api/azure/devops/wit/work-items/delete
# Params:
# - project_name: The friendly name or guid of the project
# - id: ID of the work item
def delete_workitem(PAT: str, organization_url: str, project_name: str, id: str):
    headers = {
        "Authorization": create_auth_header_ado(PAT),
        "Content-Type": "application/json",
    }
    uri_api = "{0}/{1}/_apis/wit/workitems/{2}?$expand=All&api-version=6.0".format(
        organization_url, project_name, id
    )
    response = requests.delete(url=uri_api, headers=headers)
    return response.json()


### Comments - Get
# https://docs.microsoft.com/en-us/rest/api/azure/devops/wit/comments/get-comments
# Params:
# - project_name: The friendly name or guid of the project
# - id: ID of the work item
def get_comments(PAT: str, organization_url: str, project_name: str, id: str):
    headers = {
        "Authorization": create_auth_header_ado(PAT),
        "Content-Type": "application/json",
    }
    uri_api = "{0}/{1}/_apis/wit/workitems/{2}/comments?$expand=All&api-version=6.0-preview.3".format(
        organization_url, project_name, id
    )
    response = requests.get(url=uri_api, headers=headers)
    return response.json()["comments"]


def create_auth_header_ado(PAT):
    return "Basic " + str(base64.b64encode(bytes(":" + PAT, "ascii")), "ascii")

###########################
###### JIRA REST API ######
###########################

def list_issues(API_token: str, email: str, jira_url: str, JQL_query: str):
    api = "{0}/rest/api/2/search?jql={1}&fields=attachment,summary,description,comment,assignee,parent,issuelinks,subtasks,fixVersions,created,updated,priority,status,customfield_10066,customfield_10067,customfield_10077,customfield_10101,customfield_10103,customfield_10082,customfield_10084".format(
        jira_url, JQL_query
    )
    headers = {
        "Accept": "application/json",
        "Authorization": create_auth_header_jira(email, API_token)
    }
    response = requests.get(url=api, headers=headers)
    return response.json()

def list_releases(API_token: str, email: str, jira_url: str, jira_project: str):
    api = "{0}/rest/api/2/project/{1}/version?expand=*".format(
        jira_url, jira_project
    )
    headers = {
        "Accept": "application/json",
        "Authorization": create_auth_header_jira(email, API_token)
    }
    response = requests.get(url=api, headers=headers)
    return response.json()

def get_remote_links(API_token: str, email: str, jira_url: str, issue_key: str):
    api = "{0}/rest/api/2/issue/{1}/remotelink".format(
        jira_url, issue_key
    )
    headers = {
        "Accept": "application/json",
        "Authorization": create_auth_header_jira(email, API_token)
    }
    response = requests.get(url=api, headers=headers)
    return response.json()

def create_auth_header_jira(username: str, token: str):
    if auth_method.lower() == "basic":
        return "Basic " + str(base64.b64encode(bytes(username + ":" + token, "ascii")), "ascii")
    elif auth_method.lower() == "token":
        return "Bearer " + token
    else:
        print(f"ERROR: auth method '{auth_method}' not implemented")

##########################
###### TEST HELPERS ######
##########################

def do_error(error_msg: str):
    error(error_msg)
    return 1

def test_user(jira_field_key: str, ado_field_key: str):
    if (
        jira_field_key in jira_issue["fields"]
        and jira_issue["fields"][jira_field_key] != None
        and jira_issue["fields"][jira_field_key][account_id_field] in user_map
    ):
        if ado_field_key not in ado_work_item["fields"]:
            ec = do_error(
                "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                    jira_issue_mapped_title,
                    ado_field_key,
                    jira_issue["fields"][jira_field_key]["displayName"],
                    "",
                )
            )
            return ec

        else:
            if "uniqueName" not in ado_work_item["fields"][ado_field_key]:
                ec = do_error(
                    "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                        jira_issue_mapped_title,
                        ado_field_key,
                        jira_issue["fields"][jira_field_key]["displayName"],
                        "",
                    )
                )
                return ec

            elif (
                ado_work_item["fields"][ado_field_key]["uniqueName"]
                != user_map[jira_issue["fields"][jira_field_key][account_id_field]].rstrip()
            ):
                ec = do_error(
                    "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                        jira_issue_mapped_title,
                        ado_field_key,
                        user_map[
                            jira_issue["fields"][jira_field_key][account_id_field]
                        ].rstrip(),
                        ado_work_item["fields"][ado_field_key]["uniqueName"],
                    )
                )
                return ec
    return None

def test_date(jira_field_key: str, ado_field_key: str):
    jira_changed_date = dateparser.parse(jira_issue["fields"][jira_field_key]).utcnow()
    ado_changed_date = dateparser.parse(
        ado_work_item["fields"][ado_field_key]
    ).utcnow()
    total_seconds = (jira_changed_date - ado_changed_date).total_seconds()
    if abs(total_seconds) > 2.0:
        ec = do_error(
            "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                jira_issue_mapped_title,
                ado_field_key,
                jira_issue["fields"][jira_field_key],
                ado_work_item["fields"][ado_field_key],
            )
        )
        return ec
    return None

def test_field_named(jira_field_key: str, ado_field_key: str):
    if (
        jira_field_key in jira_issue["fields"]
        and jira_issue["fields"][jira_field_key] != []
    ):
        if (
            ado_work_item["fields"][ado_field_key]
            != jira_issue["fields"][jira_field_key][0]["name"]
        ):
            ec = do_error(
                "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                    jira_issue_mapped_title,
                    ado_field_key,
                    jira_issue["fields"][jira_field_key][0]["name"],
                    ado_work_item["fields"][ado_field_key],
                )
            )
            return ec
    return None

def test_field_simple(jira_field_key: str, ado_field_key: str):
    if (
        jira_field_key in jira_issue["fields"]
        and jira_issue["fields"][jira_field_key] != None
    ):
        if (
            ado_work_item["fields"][ado_field_key]
            != jira_issue["fields"][jira_field_key]
        ):
            ec = do_error(
                "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                    jira_issue_mapped_title,
                    ado_field_key,
                    jira_issue["fields"][jira_field_key][0]["name"],
                    ado_work_item["fields"][ado_field_key],
                )
            )
            return ec
    return None

####################
###### CONFIG ######
####################

print("Argument List:", str(sys.argv))

ado_organization_url: str = sys.argv[1]
ado_project_name: str = sys.argv[2]
ado_api_token: str = sys.argv[3]

jira_url: str = sys.argv[4]
jira_email: str = sys.argv[5]
jira_api_token: str = sys.argv[6]
jira_project: str = sys.argv[7]

user_mapping_file_path = sys.argv[8]

auth_method = sys.argv[9]

#####################
###### PROGRAM ######
#####################

if ".atlassian.net" in jira_url:
    account_id_field = "accountId"
    do_verify_reporter = True
else:
    account_id_field = "emailAddress"
    do_verify_reporter = False

# Set queries
ado_wiql_query: str = (
    "SELECT [Id] FROM WorkItems WHERE [System.TeamProject] = '{0}'".format(
        ado_project_name
    )
)
jira_jql_query: str = 'project = "{0}" ORDER BY created DESC'.format(jira_project)

# Get issues/work items
jira_issues_json: list = list_issues(
    jira_api_token, jira_email, jira_url, jira_jql_query
)
jira_releases: list = list_releases(
        jira_api_token, jira_email, jira_url, jira_project
)
ado_work_items_json: list = list_workitems_by_wiql(
    ado_api_token, ado_organization_url, ado_project_name, ado_wiql_query
)

exit_code = 0

ado_work_items_by_id: dict = {}

# Parse user mapping file
user_mapping_file = open(user_mapping_file_path, "r")
user_map_list = map(lambda x: x.split("="), user_mapping_file.readlines())
user_map = {}
for entry in user_map_list:
    user_map[entry[0]] = entry[1]
user_mapping_file.close()

# Get work items
ado_work_items: list = []
for ado_work_item_key in ado_work_items_json["workItems"]:
    # Cache work items
    if ado_work_item_key["id"] not in ado_work_items_by_id.keys():
        ado_work_items_by_id[ado_work_item_key["id"]] = get_workitem(
            ado_api_token,
            ado_organization_url,
            ado_project_name,
            ado_work_item_key["id"],
        )
    ado_work_item = ado_work_items_by_id[ado_work_item_key["id"]]
    ado_work_items.append(ado_work_item)

# Check issue count
if len(jira_issues_json) != len(ado_work_items_json):
    exit_code = do_error("Jira issue count does not match ADO work item count")

for jira_issue in jira_issues_json["issues"]:
    issue_found_in_ADO: bool = False
    jira_issue_mapped_title = "[{0}] {1}".format(
        jira_issue["key"], jira_issue["fields"]["summary"]
    )

    for ado_work_item in ado_work_items:
        # Compare title
        if ado_work_item["fields"]["System.Title"] == jira_issue_mapped_title or (
            len(ado_work_item["fields"]["System.Title"]) >= 255
            and ado_work_item["fields"]["System.Title"]
            == jira_issue_mapped_title[0:252] + "..."
        ):
            issue_found_in_ADO = True
        else:
            continue

        # Compare description
        if (
            "description" in jira_issue["fields"]
            and jira_issue["fields"]["description"] != None
            and jira_issue["fields"]["description"].strip() != ""
        ):
            if (
                ado_work_item["fields"]["System.Description"]
                in jira_issue["fields"]["description"]
            ):
                exit_code = do_error(
                    "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                        jira_issue_mapped_title,
                        "System.Description",
                        jira_issue["fields"]["description"],
                        ado_work_item["fields"]["System.Description"],
                    )
                )

            # Test unformatted attachments
            if (
                "https://dev.azure.com/secure/attachment/"
                in ado_work_item["fields"]["System.Description"]
            ):
                exit_code = do_error(
                    "Problem for Jira issue '{0}': field '{1}': an unformatted attachment link was detected".format(
                        jira_issue_mapped_title, "System.Description"
                    )
                )

            # Test unformatted links to Jira issues
            if (
                "href=\\\"https://solidifydemo.atlassian.net/browse/AGILEDEMO-"
                in ado_work_item["fields"]["System.Description"]
            ):
                exit_code = do_error(
                    "Problem for Jira issue '{0}': field '{1}': an unformatted issue link was detected".format(
                        jira_issue_mapped_title, "System.Description"
                    )
                )

        # Compare custom HTML rendered field
        if (
            "customfield_10066" in jira_issue["fields"]
            and jira_issue["fields"]["customfield_10066"] != None
            and jira_issue["fields"]["customfield_10066"].strip() != ""
        ):
            if (
                ado_work_item["fields"]["Custom.CustomHtml"]
                in jira_issue["fields"]["customfield_10066"]
            ):
                exit_code = do_error(
                    "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                        jira_issue_mapped_title,
                        "Custom.CustomHtml",
                        jira_issue["fields"]["customfield_10066"],
                        ado_work_item["fields"]["Custom.CustomHtml"],
                    )
                )

            # Test unformatted attachments
            if (
                "https://dev.azure.com/secure/attachment/"
                in ado_work_item["fields"]["Custom.CustomHtml"]
            ):
                exit_code = do_error(
                    "Problem for Jira issue '{0}': field '{1}': an unformatted attachment link was detected".format(
                        jira_issue_mapped_title, "Custom.CustomHtml"
                    )
                )

        # Compare status
        if "status" in jira_issue["fields"] and jira_issue["fields"]["status"] != None:
            if (
                (
                    jira_issue["fields"]["status"]["name"] == "Klart"
                    and not (
                        ado_work_item["fields"]["System.State"] == "Resolved"
                        or ado_work_item["fields"]["System.State"] == "Closed"
                    )
                )
                or (
                    jira_issue["fields"]["status"]["name"] == "Pågående"
                    and ado_work_item["fields"]["System.State"] != "Active"
                )
                or (
                    jira_issue["fields"]["status"]["name"] == "Att göra"
                    and ado_work_item["fields"]["System.State"] != "New"
                )
            ):
                exit_code = do_error(
                    "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                        jira_issue_mapped_title,
                        "System.State",
                        jira_issue["fields"]["status"]["name"],
                        ado_work_item["fields"]["System.State"],
                    )
                )

        # Compare attachment count
        if (
            "attachment" in jira_issue["fields"]
            and jira_issue["fields"]["attachment"] != None
        ):
            jira_attachments_filtered = list(
                filter(
                    lambda a: a["size"] < 60000000, jira_issue["fields"]["attachment"]
                )
            )
            jira_attachment_count = len(jira_attachments_filtered)
            if jira_attachment_count > 0:
                ado_attachment_count = len(
                    list(
                        filter(
                            lambda x: x["rel"] == "AttachedFile",
                            ado_work_item["relations"],
                        )
                    )
                )
                if (
                    ado_attachment_count < 100
                    and ado_attachment_count != jira_attachment_count
                ):
                    exit_code = do_error(
                        "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                            jira_issue_mapped_title,
                            "AttachmentCount",
                            jira_attachment_count,
                            ado_attachment_count,
                        )
                    )

        # Compare area path
        if (
            ado_work_item["fields"]["System.AreaPath"]
            != "AzureDevOps-Jira-Migrator-Smoke-Tests\Migrated"
        ):
            exit_code = do_error(
                "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                    jira_issue_mapped_title,
                    "System.AreaPath",
                    ado_work_item["fields"]["System.AreaPath"],
                    "AzureDevOps-Jira-Migrator-Smoke-Tests\\Migrated",
                )
            )

        # Compare comment count
        if (
            "comment" in jira_issue["fields"]
            and len(jira_issue["fields"]["comment"]["comments"]) > 0
        ):
            jira_comment_count = len(jira_issue["fields"]["comment"]["comments"])
            if jira_comment_count > 0:
                ado_comments = get_comments(
                    ado_api_token,
                    ado_organization_url,
                    ado_project_name,
                    ado_work_item["id"],
                )
                ado_comments_filtered = list(
                    filter(
                        lambda x: "Added link(s): [Added]" not in x["text"],
                        ado_comments,
                    )
                )
                ado_comment_count = len(ado_comments_filtered)
                if ado_comment_count != jira_comment_count:
                    exit_code = do_error(
                        "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                            jira_issue_mapped_title,
                            "CommentCount",
                            jira_comment_count,
                            ado_comment_count,
                        )
                    )

        # Compare AssignedTo
        if test_user("assignee", "System.AssignedTo") != None:
            exit_code = 1
        
        # Compare parent
        if "parent" in jira_issue["fields"] and jira_issue["fields"]["parent"] != None:
            jira_parent = jira_issue["fields"]["parent"]
            jira_parent_mapped_title = "[{0}] {1}".format(
                jira_parent["key"], jira_parent["fields"]["summary"]
            )

            if "relations" not in ado_work_item:
                exit_code = do_error(
                    "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                        jira_issue_mapped_title, "Parent", "", jira_parent_mapped_title
                    )
                )
            else:
                ado_relations_filtered = list(
                    filter(
                        lambda x: x["rel"] == "System.LinkTypes.Hierarchy-Reverse",
                        ado_work_item["relations"],
                    )
                )
                if len(ado_relations_filtered) == 0:
                    exit_code = do_error(
                            "Problem for Jira issue '{0}': field '{1}' did not exist when it should.".format(
                                jira_issue_mapped_title,
                                "Parent"
                            )
                        )
                else:
                    ado_parent_id = int(ado_relations_filtered[0]["url"].split("/")[-1])
                    ado_parent_work_item = ado_work_items_by_id[ado_parent_id]
                    if (
                        jira_parent_mapped_title
                        != ado_parent_work_item["fields"]["System.Title"]
                    ):
                        exit_code = do_error(
                            "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                                jira_issue_mapped_title,
                                "Parent",
                                ado_parent_work_item["fields"]["System.Title"],
                                jira_parent_mapped_title,
                            )
                        )

        # Compare related issues
        if (
            "issuelinks" in jira_issue["fields"]
            and len(jira_issue["fields"]["issuelinks"]) > 0
        ):
            jira_linked_issues = jira_issue["fields"]["issuelinks"]
            jira_related_issues_filtered = list(
                filter(lambda x: x["type"]["name"] == "Relates", jira_linked_issues)
            )
            if len(jira_related_issues_filtered) > 0:
                if "relations" not in ado_work_item:
                    exit_code = do_error(
                        "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                            jira_issue_mapped_title,
                            "Linked Issue count",
                            "0",
                            str(len(jira_linked_issues)),
                        )
                    )
                else:
                    ado_relations_filtered = list(
                        filter(
                            lambda x: x["rel"] == "System.LinkTypes.Related",
                            ado_work_item["relations"],
                        )
                    )
                    for jira_related_issue in jira_related_issues_filtered:
                        if "inwardIssue" in jira_related_issue:
                            jira_related_issue_mapped_title = "[{0}] {1}".format(
                                jira_related_issue["inwardIssue"]["key"],
                                jira_related_issue["inwardIssue"]["fields"]["summary"],
                            )
                        elif "outwardIssue" in jira_related_issue:
                            jira_related_issue_mapped_title = "[{0}] {1}".format(
                                jira_related_issue["outwardIssue"]["key"],
                                jira_related_issue["outwardIssue"]["fields"]["summary"],
                            )
                        ado_relations_filtered_final = list(
                            filter(
                                lambda x: ado_work_items_by_id[
                                    int(x["url"].split("/")[-1])
                                ]["fields"]["System.Title"]
                                == jira_related_issue_mapped_title,
                                ado_relations_filtered,
                            )
                        )
                        if len(ado_relations_filtered_final) != len(
                            jira_related_issues_filtered
                        ):
                            exit_code = do_error(
                                "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                                    jira_issue_mapped_title,
                                    "Related issues count",
                                    len(ado_relations_filtered_final),
                                    len(jira_related_issues_filtered),
                                )
                            )

        # Compare subtasks
        if (
            "subtasks" in jira_issue["fields"]
            and len(jira_issue["fields"]["subtasks"]) > 0
        ):
            jira_linked_issues = jira_issue["fields"]["subtasks"]

            if "relations" not in ado_work_item:
                exit_code = do_error(
                    "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                        jira_issue_mapped_title,
                        "Subtask (child) count",
                        "0",
                        str(len(jira_linked_issues)),
                    )
                )
            else:
                ado_relations_filtered = list(
                    filter(
                        lambda x: x["rel"] == "System.LinkTypes.Hierarchy-Forward",
                        ado_work_item["relations"],
                    )
                )
                for jira_related_issue in jira_linked_issues:
                    jira_related_issue_mapped_title = "[{0}] {1}".format(
                        jira_related_issue["key"],
                        jira_related_issue["fields"]["summary"],
                    )
                    ado_relations_filtered_final = list(
                        filter(
                            lambda x: ado_work_items_by_id[
                                int(x["url"].split("/")[-1])
                            ]["fields"]["System.Title"]
                            == jira_related_issue_mapped_title,
                            ado_relations_filtered,
                        )
                    )
                    if len(ado_relations_filtered_final) != 1:
                        exit_code = do_error(
                            "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                                jira_issue_mapped_title,
                                "Subtack (child) count",
                                len(ado_relations_filtered_final),
                                len(jira_linked_issues),
                            )
                        )

        # Compare Fixversions
        if (
            "fixVersions" in jira_issue["fields"]
            and jira_issue["fields"]["fixVersions"] != []
            and jira_issue["fields"]["fixVersions"][0]["name"] == "2021.2.0.296"
        ):
            if test_field_named("fixVersions", "Custom.FixVersion") != None:
                exit_code = 1

        # Compare createdDate
        if test_date("created", "System.CreatedDate") != None:
            exit_code = 1

        # Compare changedDate
        if test_date("updated", "System.ChangedDate") != None:
            exit_code = 1

        # Compare Reporter
        if do_verify_reporter:
            if test_user("reporter", "Custom.Reporter") != None:
                exit_code = 1

        # Compare Custom UserPicker
        if test_user("alexander-testar-custom-userpicker", "Custom.CustomUserPicker") != None:
            exit_code = 1

        # Compare Story points
        if test_field_named("customfield_10014", "Microsoft.VSTS.Scheduling.StoryPoints") != None:
            exit_code = 1

        # Compare priority
        if "priority" in jira_issue["fields"] and jira_issue["fields"]["priority"] != None:
            if (
                (
                    jira_issue["fields"]["priority"]["name"] == "Highest"
                    and ado_work_item["fields"]["Microsoft.VSTS.Common.Priority"] != 1
                )
                or (
                    jira_issue["fields"]["priority"]["name"] == "High"
                    and ado_work_item["fields"]["Microsoft.VSTS.Common.Priority"] != 2
                )
                or (
                    jira_issue["fields"]["priority"]["name"] == "Medium"
                    and ado_work_item["fields"]["Microsoft.VSTS.Common.Priority"] != 3
                )
                or (
                    jira_issue["fields"]["priority"]["name"] == "Low"
                    and ado_work_item["fields"]["Microsoft.VSTS.Common.Priority"] != 3
                )
                or (
                    jira_issue["fields"]["priority"]["name"] == "Lowest"
                    and ado_work_item["fields"]["Microsoft.VSTS.Common.Priority"] != 4
                )
            ):
                exit_code = do_error(
                    "Problem for Jira issue '{0}': field '{1}' did not match the target work item. ('{2}' vs '{3}')".format(
                        jira_issue_mapped_title,
                        "Microsoft.VSTS.Common.Priority",
                        jira_issue["fields"]["priority"]["name"],
                        ado_work_item["fields"]["Microsoft.VSTS.Common.Priority"],
                    )
                )

        # Compare alexander-testar-plain-text
        if test_field_simple("customfield_10067", "Custom.CustomPlainText") != None:
            exit_code = 1

        # Compare alexander-testar-custom-number-field
        if test_field_simple("customfield_10077", "Custom.CustomNumber") != None:
            exit_code = 1

        # Compare alexander-testar-custom-rating
        if test_field_simple("customfield_10101", "Custom.CustomRating") != None:
            exit_code = 1

        # Compare alexander-testar-custom-slider
        if test_field_simple("customfield_10103", "Custom.CustomSlider") != None:
            exit_code = 1

        # Compare alexander-testar-custom-url-field
        if test_field_simple("customfield_10082", "Custom.CustomUrlField") != None:
            exit_code = 1

        # Compare alexander-testar-custom-custom-formula
        if test_field_simple("customfield_10084", "Custom.CustomFormula") != None:
            exit_code = 1

#alexander-testar-custom-project-picker-single-project   customfield_10100       CustomProjectPickerSingleProject    test_field_named

exit(exit_code)