---
title: Local PDF indexing
description: Extract, chunk, embed, and index local PDFs using parent and child Azure AI Search indexes without skillsets.
---

## Index a local PDF

The application uses either Document Intelligence's `prebuilt-layout` model or
Content Understanding to extract text from a PDF on disk, including scanned pages.
It splits the extracted text
into chunks of up to 2,000 UTF-16 characters with 200-character overlap, generates
1536-dimensional Azure OpenAI embeddings, and pushes documents to Azure AI Search.
It does not create Azure AI Search indexers, data sources, or skillsets.

* `my-parent-index` stores a PDF's `parent_id` and filename-derived `title`
* `my-child-index` stores `chunk_id`, `parent_id`, `chunk`, and `chunk_vector`

Configure endpoints in [appsettings.local.json](./appsettings.local.json), or
set environment variables using double underscores in place of colons:

* `AzureAISearch__Endpoint`
* `PdfExtraction__Provider`, either `DocumentIntelligence` (default) or `ContentUnderstanding`
* `DocumentIntelligence__Endpoint`, when selecting Document Intelligence
* `ContentUnderstanding__Endpoint`, when selecting Content Understanding
* `ContentUnderstanding__AnalyzerId`, defaulting in the sample configuration to `prebuilt-documentSearch`
* `AzureOpenAI__Endpoint`
* `AzureOpenAI__EmbeddingModel`, the deployment name of a `text-embedding-3-small`
  model, matching the child index's query-time vectorizer and 1536 dimensions

Only the selected extraction provider needs to be configured. Content Understanding
returns Markdown, which goes through the same chunking, Azure OpenAI embedding,
and parent-child indexing pipeline as Document Intelligence text.

Search uses your Azure CLI identity. Both extraction providers and Azure OpenAI also
use that identity unless you configure `DocumentIntelligence__ApiKey`,
`ContentUnderstanding__ApiKey`, or `AzureOpenAI__ApiKey`. Keep API keys out of source control.

The child index's HNSW profile uses an Azure OpenAI vectorizer with the configured
endpoint, deployment, and optional `AzureOpenAI__ApiKey` for text-to-vector queries.
Without that key, query-time vectorization uses the search service's system-assigned
managed identity, not your Azure CLI identity. Enable that identity and grant it
Cognitive Services OpenAI User on the Azure OpenAI resource.

Both indexes have a semantic configuration named `default`. The parent index uses
`title` as its title field; the child index prioritizes `chunk` as content.
Enable the semantic ranker on your search service and use semantic queries to
apply this configuration. Fields from the sample such as `summary`, `model`,
`manufacturer`, and `class` are not part of the PDF index schema.

For identity authentication, run `az login` and grant the relevant resource roles:

* Search Service Contributor and Search Index Data Contributor for Azure AI Search
* Cognitive Services User for Document Intelligence
* Cognitive Services User for Content Understanding, when selected
* Cognitive Services OpenAI User for Azure OpenAI

The PDF contents are sent to your configured Azure services. The local file does
not need to be uploaded to Blob Storage.

```powershell
$env:RunMode = "Indexing"
dotnet run -- "C:\Documents\report.pdf"
```

To choose Content Understanding for a run:

```powershell
$env:PdfExtraction__Provider = "ContentUnderstanding"
$env:ContentUnderstanding__Endpoint = "https://<your-resource>.services.ai.azure.com/"
$env:ContentUnderstanding__AnalyzerId = "prebuilt-documentSearch"
dotnet run -- "C:\Documents\report.pdf"
```

Alternatively, set `PdfExtraction:Provider` in your local configuration file.
The Content Understanding client uses the GA `2025-11-01` service API.
Your Microsoft Foundry resource must support Content Understanding and have the
model deployment defaults required by the selected analyzer configured before
running. The application does not create analyzers or alter model mappings.
See the [Content Understanding setup guide](https://learn.microsoft.com/en-us/azure/ai-services/content-understanding/quickstart/use-ai-foundry)
for resource setup.

The application creates or updates both indexes before indexing. The indexes
must have compatible schemas; existing incompatible schemas are not deleted.
Press Ctrl+C to cancel.

## Search indexed chunks

Set `RunMode` to `Search` and pass the query as the single command-line argument:

```powershell
$env:RunMode = "Search"
dotnet run -- "How do I reset the device?"
```

[ChildIndexSearch.cs](./ChildIndexSearch.cs) searches `chunk` text and uses the
child index's vectorizer to search `chunk_vector`. It requests 50 vector
candidates for semantic reranking with the `default` configuration and returns
the top 10 matches. The console prints each match's chunk ID, parent ID, search
score, and chunk text, or a message when no chunks match.

Search mode does not create indexes or initialize PDF extraction and embedding
clients. The child index must already exist with its vectorizer and semantic
configuration. Configure `AzureAISearch__Endpoint`, sign in with `az login`, and
grant your identity Search Index Data Reader. Query-time vectorization uses the
authentication configured on the index's vectorizer; semantic ranker must be
enabled on the search service. Service errors propagate without falling back
to another search mode. Press Ctrl+C to cancel.

## Reindexing and failures

The parent key is a SHA-256 hash of the normalized absolute file path. Reindexing
that path overwrites its parent and matching child documents. Obsolete-chunk
cleanup is currently disabled in the indexer, so reindexing a shorter PDF leaves
older excess chunks in the child index. Moving a file creates a new parent; it does not
delete the original parent's documents. Do not index the same path concurrently.

Azure AI Search does not provide a transaction across the two indexes. Service
errors and individual upload failures propagate rather than reporting success.
An interrupted run can leave a partial update; rerun the same path to complete it.
Search updates can take time to become visible to queries, so wait for a previous
run's writes to become searchable before reindexing.

## Package restore

[NuGet.Config](./NuGet.Config) uses the Microsoft package feed proxy and maps all
packages to that single source. The project-local configuration overrides inherited
sources and mappings to avoid duplicate feed registrations under different names.
It does not change your user-wide or machine-wide NuGet configuration.

```powershell
dotnet restore .\ai-search.csproj
```
