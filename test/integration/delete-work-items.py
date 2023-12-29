import sys
import requests
import base64

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
        "Authorization": create_auth_header(PAT),
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


### Work Items - Delete
# https://docs.microsoft.com/en-us/rest/api/azure/devops/wit/work-items/delete
# Params:
# - project_name: The friendly name or guid of the project
# - id: ID of the work item
def delete_workitem(PAT: str, organization_url: str, project_name: str, id: str):
    headers = {
        "Authorization": create_auth_header(PAT),
        "Content-Type": "application/json",
    }
    uri_api = "{0}/{1}/_apis/wit/workitems/{2}?$expand=All&api-version=6.0".format(
        organization_url, project_name, id
    )
    response = requests.delete(url=uri_api, headers=headers)
    return response.json()


def create_auth_header(PAT):
    return "Basic " + str(base64.b64encode(bytes(":" + PAT, "ascii")), "ascii")


####################
###### CONFIG ######
####################

print("Argument List:", str(sys.argv))

ado_organization_url: str = sys.argv[1]
ado_project_name: str = sys.argv[2]
ado_api_token: str = sys.argv[3]

#####################
###### PROGRAM ######
#####################

# Set queries
ado_wiql_query: str = (
    "SELECT [Id] FROM WorkItems WHERE [System.TeamProject] = '{0}'".format(
        ado_project_name
    )
)

# Get issues/work items
ado_work_items_json: list = list_workitems_by_wiql(
    ado_api_token, ado_organization_url, ado_project_name, ado_wiql_query
)

original_wi_count: int = len(ado_work_items_json["workItems"])

for work_item in ado_work_items_json["workItems"]:
    delete_workitem(
        ado_api_token, ado_organization_url, ado_project_name, work_item["id"]
    )

print("Deleted {0} work items from {1}".format(original_wi_count, ado_project_name))
