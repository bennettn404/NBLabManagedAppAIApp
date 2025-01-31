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
[mainTemplate](https://github.com/bennettn404/NBLabManagedAppAIApp/blob/812ef17520b46e83543e59b9f9ec62985513f5eb/nblab-ai-fa/ManagedAppDefinitions/mainTemplate.json)
and  createUiDefinition.json
[createUiDefinition](https://pages.github.com/](https://github.com/bennettn404/NBLabManagedAppAIApp/blob/812ef17520b46e83543e59b9f9ec62985513f5eb/nblab-ai-fa/ManagedAppDefinitions/createUiDefinition.json))

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
