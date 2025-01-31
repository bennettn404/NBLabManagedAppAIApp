# Azure AI Service via a Service Catalog Managed Application

## Lab description
Designed to be completed in less than an hour, this lab will walk you through the basics of creating a Service Catalog Managed Application Definition, deploying it, and then deploying an instance of the Managed Application to run an Azure AI Function App.

## Prerequisites
- Access to an Azure account which can create paid resources
- Basic knowledge of ARM templates
- Basic knowledge of deploying .NET applications

## Creating the Service Catalog Managed Application Definition
Service Catalog Managed Applications are a useful way to deploy bundles of azure infrastructure in a way that mimics native Azure deployments. This allows customers to deploy an application stack without in-depth knowledge of the underlying infrastructure and it's configuration.

A Service Catalog Managed Application Definition is comprised of two parts, the mainTemplate.json and the createUiDefinition.json
- The mainTemplate.json is purely the ARM template of that Azure infrastructure to be deployed, with parameters exposed for configuration from the deployment
- The createUiDefinition.json describes the deployment process a user will see when deploying the Managed App from the Service Catalog. This is made up of various parameter inputs to the mainTemplate.json

For example the mainTemplate to be used in this lab:
<details>
  '''
  {
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {

    "location": {
      "type": "string",
      "defaultValue": "[resourceGroup().location]"
    },
    "functionPlanName": {
      "type": "string",
      "defaultValue": "[concat('nblab-asp-', uniqueString(resourceGroup().id, deployment().name))]"
    },
    "functionAppName": {
      "type": "string",
      "defaultValue": "[concat('nblab-fa-', uniqueString(resourceGroup().id, deployment().name))]"
    },
    "aiServicesName": {
      "type": "string",
      "defaultValue": "[concat('nblab-aicv-', uniqueString(resourceGroup().id, deployment().name))]"
    },
    "functionAppRuntime": {
      "type": "string",
      "defaultValue": "dotnet-isolated"
    },
    "functionAppRuntimeVersion": {
      "type": "string",
      "defaultValue": "8.0"
    },
    "applicationZipUrl": {
      "type": "string"
    },
    "storageAccountName": {
      "type": "string",
      "defaultValue": "[concat('nblabsa', uniqueString(resourceGroup().id, deployment().name))]"
    },
    "logAnalyticsName": {
      "type": "string",
      "defaultValue": "[concat('nblab-la-', uniqueString(resourceGroup().id, deployment().name))]"
    },
    "applicationInsightsName": {
      "type": "string",
      "defaultValue": "[concat('nblab-appi-', uniqueString(resourceGroup().id, deployment().name))]"
    }
  },
  "variables": {
    "resourceToken": "[toLower(uniqueString(subscription().id, resourceGroup().name, resourceGroup().location))]",
    "deploymentStorageContainerName": "[concat('app-package-', take(parameters('functionAppName'), 32),'-', take(variables('resourceToken'), 7))]",
    "storageRoleDefinitionId": "b7e6dc6d-f1e8-4753-8033-0f276bb0955b" //Storage Blob Data Owner role

  },
  "resources": [
    {
      "type": "microsoft.operationalinsights/workspaces",
      "apiVersion": "2021-06-01",
      "name": "[parameters('logAnalyticsName')]",
      "location": "[parameters('location')]",
      "properties": {
        "retentionInDays": 30,
        "features": {
          "searchVersion": 1
        },
        "sku": {
          "name": "PerGB2018"
        }
      }
    },
    {
      "type": "Microsoft.Insights/components",
      "apiVersion": "2020-02-02",
      "name": "[parameters('applicationInsightsName')]",
      "location": "[parameters('location')]",
      "kind": "web",
      "properties": {
        "Application_Type": "web",
        "WorkspaceResourceId": "[resourceId('Microsoft.OperationalInsights/workspaces', parameters('logAnalyticsName'))]"
      },
      "dependsOn": [
        "[resourceId('Microsoft.OperationalInsights/workspaces', parameters('logAnalyticsName'))]"
      ]
    },
    {
      "type": "Microsoft.Storage/storageAccounts",
      "apiVersion": "2023-01-01",
      "name": "[parameters('storageAccountName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "Standard_LRS",
        "tier": "Standard"
      },
      "kind": "StorageV2",
      "properties": {
        "accessTier": "Hot"
      }
    },
    {
      "type": "Microsoft.Web/serverfarms",
      "apiVersion": "2023-12-01",
      "name": "[parameters('functionPlanName')]",
      "location": "[parameters('location')]",
      "kind": "functionapp",
      "sku": {
        "name": "Y1",
        "tier": "Dynamic",
        "size": "Y1",
        "family": "Y",
        "capacity": 0
      },
      "properties": {
        "perSiteScaling": false,
        "elasticScaleEnabled": false,
        "maximumElasticWorkerCount": 1,
        "isSpot": false,
        "reserved": true,
        "isXenon": false,
        "hyperV": false,
        "targetWorkerCount": 0,
        "targetWorkerSizeId": 0,
        "zoneRedundant": false
      }
    },
    {
      "type": "Microsoft.Web/sites",
      "apiVersion": "2023-12-01",
      "name": "[parameters('functionAppName')]",
      "location": "[parameters('location')]",
      "kind": "functionapp,linux",
      "identity": {
        "type": "SystemAssigned"
      },
      "properties": {
        "serverFarmId": "[resourceId('Microsoft.Web/serverfarms', parameters('functionPlanName'))]",
        "siteConfig": {
          "linuxFxVersion": "[concat(parameters('functionAppRuntime'),'|', parameters('functionAppRuntimeVersion'))]",
          "appSettings": [
            {
              "name": "AzureWebJobsStorage__accountName",
              "value": "[parameters('storageAccountName')]"
            },
            {
              "name": "APPLICATIONINSIGHTS_CONNECTION_STRING",
              "value": "[reference(resourceId('Microsoft.Insights/components', parameters('applicationInsightsName')), '2020-02-02').ConnectionString]"
            },
            {
              "name": "FUNCTIONS_EXTENSION_VERSION",
              "value": "~4"
            },
            {
              "name": "WEBSITE_RUN_FROM_PACKAGE",
              "value": "[parameters('applicationZipUrl')]"
            },
            {
              "name": "FUNCTIONS_WORKER_RUNTIME",
              "value": "dotnet-isolated"
            },
            {
              "name": "VISION_ENDPOINT",
              "value": "[concat('https://', parameters('location'), '.api.cognitive.microsoft.com/')]"
            },
            {
              "name": "VISION_KEY",
              "value": "[listkeys(resourceId('Microsoft.CognitiveServices/accounts', parameters('aiServicesName')), '2023-05-01').key1]"
            },
            {
              "name": "AzureWebJobsStorage",
              "value": "[format('DefaultEndpointsProtocol=https;AccountName={0};EndpointSuffix={1};AccountKey={2}', parameters('storageAccountName'), environment().suffixes.storage, listKeys(resourceId('Microsoft.Storage/storageAccounts', parameters('storageAccountName')), '2022-05-01').keys[0].value)]"
            },
            {
              "name": "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING",
              "value": "[format('DefaultEndpointsProtocol=https;AccountName={0};EndpointSuffix={1};AccountKey={2}', parameters('storageAccountName'), environment().suffixes.storage, listKeys(resourceId('Microsoft.Storage/storageAccounts', parameters('storageAccountName')), '2022-05-01').keys[0].value)]"
            },
            {
              "name": "WEBSITE_CONTENTSHARE",
              "value": "[toLower(parameters('functionAppName'))]"
            }
          ]
        }
      },
      "dependsOn": [
        "[resourceId('Microsoft.Web/serverfarms', parameters('functionPlanName'))]",
        "[resourceId('Microsoft.Storage/storageAccounts', parameters('storageAccountName'))]",
        "[resourceId('Microsoft.Insights/components', parameters('applicationInsightsName'))]",
        "[resourceId('Microsoft.CognitiveServices/accounts', parameters('aiServicesName'))]"
      ]
    },
    {
      "type": "Microsoft.Web/sites/slots",
      "apiVersion": "2023-12-01",
      "name": "[format('{0}/{1}', parameters('functionAppName'), 'DeploymentSlot1')]",
      "location": "[parameters('location')]",
      "kind": "functionapp,linux",
      "identity": {
        "type": "SystemAssigned"
      },
      "properties": {
        "serverFarmId": "[resourceId('Microsoft.Web/serverfarms', parameters('functionPlanName'))]",
        "siteConfig": {
          "linuxFxVersion": "[concat(parameters('functionAppRuntime'),'|', parameters('functionAppRuntimeVersion'))]",
          "appSettings": [
            {
              "name": "APPLICATIONINSIGHTS_CONNECTION_STRING",
              "value": "[reference(resourceId('Microsoft.Insights/components', parameters('applicationInsightsName')), '2020-02-02').ConnectionString]"
            },
            {
              "name": "FUNCTIONS_EXTENSION_VERSION",
              "value": "~4"
            },
            {
              "name": "FUNCTIONS_WORKER_RUNTIME",
              "value": "dotnet-isolated"
            },
            {
              "name": "VISION_ENDPOINT",
              "value": "[concat('https://', parameters('location'), '.api.cognitive.microsoft.com/')]"
            },
            {
              "name": "VISION_KEY",
              "value": "[listkeys(resourceId('Microsoft.CognitiveServices/accounts', parameters('aiServicesName')), '2023-05-01').key1]"
            },
            {
              "name": "AzureWebJobsStorage",
              "value": "[format('DefaultEndpointsProtocol=https;AccountName={0};EndpointSuffix={1};AccountKey={2}', parameters('storageAccountName'), environment().suffixes.storage, listKeys(resourceId('Microsoft.Storage/storageAccounts', parameters('storageAccountName')), '2022-05-01').keys[0].value)]"
            },
            {
              "name": "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING",
              "value": "[format('DefaultEndpointsProtocol=https;AccountName={0};EndpointSuffix={1};AccountKey={2}', parameters('storageAccountName'), environment().suffixes.storage, listKeys(resourceId('Microsoft.Storage/storageAccounts', parameters('storageAccountName')), '2022-05-01').keys[0].value)]"
            },
            {
              "name": "WEBSITE_CONTENTSHARE",
              "value": "[toLower(parameters('functionAppName'))]"
            }
          ]
        }
      },
      "dependsOn": [
        "[resourceId('Microsoft.Web/serverfarms', parameters('functionPlanName'))]",
        "[resourceId('Microsoft.Storage/storageAccounts', parameters('storageAccountName'))]",
        "[resourceId('Microsoft.Insights/components', parameters('applicationInsightsName'))]",
        "[resourceId('Microsoft.Web/sites', parameters('functionAppName'))]",
        "[resourceId('Microsoft.CognitiveServices/accounts', parameters('aiServicesName'))]"
      ]
    },
    {
      "type": "Microsoft.CognitiveServices/accounts",
      "apiVersion": "2023-05-01",
      "name": "[parameters('aiServicesName')]",
      "location": "[parameters('location')]",
      "identity": {
        "type": "SystemAssigned"
      },
      "kind": "ComputerVision",
      "sku": {
        "name": "S1"
      },
      "properties": {
        "publicNetworkAccess": "Enabled"
      }
    }
  ]
}
  '''
</details>
and  createUiDefinition.json
<details>
  '''
  {
  "$schema": "https://schema.management.azure.com/schemas/0.1.2-preview/CreateUIDefinition.MultiVm.json#",
  "handler": "Microsoft.Azure.CreateUIDef",
  "version": "0.1.2-preview",
  "parameters": {
    "basics": [],
    "steps": [
      {
        "name": "functionAppConfig",
        "label": "Function App Configuration",
        "elements": [
          {
            "name": "functionAppConfigSection1",
            "type": "Microsoft.Common.Section",
            "label": "Function App Runtime Details",
            "elements": [
              {

                "name": "runtime",
                "type": "Microsoft.Common.DropDown",
                "label": "Runtime",
                "placeholder": "",
                "defaultValue": [ "dotnet-isolated" ],
                "toolTip": "The runtime the function will use",
                "defaultDescription": "A value for selection",
                "constraints": {
                  "allowedValues": [
                    {
                      "label": ".NET",
                      "description": "dotnet",
                      "value": "dotnet-isolated"
                    },
                    {
                      "label": "Java",
                      "description": "java",
                      "value": "java"
                    }
                  ],
                  "required": true
                },
                "visible": true
              },
              {
                "name": "runtimeNET",
                "type": "Microsoft.Common.DropDown",
                "label": ".NET Runtime",
                "placeholder": "",
                "toolTip": "The .NET runtime the function will use",
                "defaultDescription": "A value for selection",
                "constraints": {
                  "allowedValues": [
                    {
                      "label": "8.0",
                      "description": ".NET 8.0",
                      "value": "8.0"
                    },
                    {
                      "label": "9.0",
                      "description": ".NET 9.0",
                      "value": "9.0"
                    }
                  ],
                  "required": true
                },
                "visible": "[equals(steps('functionAppConfig').functionAppConfigSection1.runtime,'dotnet-isolated')]"
              },
              {
                "name": "runtimeJava",
                "type": "Microsoft.Common.DropDown",
                "label": "Java Runtime",
                "placeholder": "",
                "toolTip": "The Java runtime the function will use",
                "defaultDescription": "A value for selection",
                "constraints": {
                  "allowedValues": [
                    {
                      "label": "8",
                      "description": "Java 8",
                      "value": "8"
                    },
                    {
                      "label": "11",
                      "description": "Java 11",
                      "value": "11"
                    },
                    {
                      "label": "17",
                      "description": "Java 17",
                      "value": "17"
                    },
                    {
                      "label": "21",
                      "description": "Java 21",
                      "value": "21"
                    }
                  ],
                  "required": true
                },
                "visible": "[equals(steps('functionAppConfig').functionAppConfigSection1.runtime,'java')]"
              },
              {
                "name": "zipDeployUrl",
                "type": "Microsoft.Common.TextBox",
                "label": "URL to deployment",
                "defaultValue": "",
                "toolTip": "Please enter a url to a zipped function app deployment",
                "placeholder": "",
                "constraints": {
                  "required": true,
                  "validationMessage": "URL is required"
                },
                "visible": true
              }
            ],
            "visible": true
          }
        ]
      },
      {
        "name": "step2",
        "label": "Step 2",
        "elements": [
        ]
      }
    ],
    "outputs": {
      "location": "[location()]",
      "functionAppRuntime": "[steps('functionAppConfig').functionAppConfigSection1.runtime]",
      "functionAppRuntimeVersion": "[coalesce(steps('functionAppConfig').functionAppConfigSection1.runtimeJava,coalesce(steps('functionAppConfig').functionAppConfigSection1.runtimeNET)]",
      "applicationZipUrl": "[steps('functionAppConfig').functionAppConfigSection1.zipDeployUrl]"
    }
  }
}
  '''
</details>
Put these in a zip archive, taking care the file names are case-sensitive, and provide a url to this zip archive (possibly using Azure storage). This has been provided in this repo.

Then deploy the definition via the Service Catalog Managed Application Definition portal, specifying which permissions the user creating the Managed App will have to the underlying resources, and the option to specify an identity with access to all created Managed Apps from that definition.

## Deploying the Service Catalog Managed Application
Once the definition is deployed, the Managed App can be deployed from the Service Catalog.
The wizard provided will mirror those provided by native Azure products and are defined by the createUIDefinition.json
Take note of the location used, some resources have capacity limitation for certain subscription types.
For the test AI Computer vision application provided, select .NET as runtime and 8.0 as version.
Then provide a url of a zip archive containing the function app deployment. This has been provided in this repo.

## Exercise 1
Modify the definition to provide Python as an available runtime.

## Exercise 2 
Modify the definition and application to use Azure OpenAI
