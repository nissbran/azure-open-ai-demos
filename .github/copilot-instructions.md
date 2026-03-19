# Project Overview

This project contains a collection of demos showcasing different functionalities of Azure OpenAI services.

## GitHub Repo Information

This repo is hosted in GitHub:
- owner: nissbran
- repo: azure-open-ai-demos

## Folder Structure

- `/docs`: Contains documentation for the project, including API specifications and user guides.
- `/src`: Contains the source code for the application, including various demos showcasing different functionalities of Azure OpenAI services.
  - `/demo1`: Basic Azure OpenAI ChatCompletion demo.
  - `/demo2`: Function Calling + RAG demo using Azure AI Search.
  - `/demo3`: Microsoft.Extensions.AI + RAG demo.
  - `/demo4`: Semantic Kernel with RAG demo.
  - `/demo5`: Semantic Kernel with Agents demo.
  - `/demo6`: Semantic Kernel + Custom MCP demo.
  - `/demo7`: GitHub MCP Server demo.
  - `/demo8`: Azure Foundry Agent Service demo.
  - `/demo9`: Multi agent orchestration demo.
  - `/demo10`: Microsoft Agent Framework demo.
  - `/demo11`: Microsoft Agent Framework with AGUI demo, including Aspire, a React frontend and .NET backend.
  - `/demo12`: Microsoft Agent Framework with Azure AI Search demo.
  - `/indexer`: SWAPI Data Indexer for RAG demos.
  - `/McpToolServer`: Custom MCP Server implementation.
- `/infrastructure`: Contains the infrastructure as code (IaC) files for deploying Azure AI Search and Azure AI Foundry.

## C# Coding Standards

### Language Features
- Use **file-scoped namespaces** for all C# files
- Enable **implicit usings** and **nullable reference types**
- Treat warnings as errors

### Code Style
- Follow the conventions in `.editorconfig`
- Use clear, descriptive XML documentation comments for public APIs
- Follow async/await patterns consistently
- Use file-scoped namespaces: `namespace ModelContextProtocol.Client;`

## Architecture

The demo applications are built as console chat applications that interact with Azure OpenAI services. They demonstrate various features such as chat completions, function calling, retrieval-augmented generation (RAG), and integration with Azure AI Search and Azure AI Foundry.

