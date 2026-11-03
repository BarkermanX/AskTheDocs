# AskTheDocs

AskTheDocs is a full-stack learning project: a document Q&A prototype for exploring retrieval-augmented generation (RAG). It currently works with a small, built-in set of UK Government Personal Independence Payment (PIP) PDFs. Users can ask questions in plain English and see an AI-generated answer alongside the source passages used to produce it.

The application combines:
- A .NET 10 ASP.NET Core API for indexing and answer generation
- A React + Vite frontend for asking questions in a simple chat-style experience
- Ollama for embeddings and LLM generation
- Qdrant for vector search
- PDF extraction and chunking using PdfPig

## Features

- Index the built-in PDF guidance documents
- Convert PDF pages into searchable text chunks
- Generate embeddings for semantic retrieval
- Expand user questions into alternative searches to improve retrieval
- Return answers grounded in relevant document passages
- Show supporting passages with document names and page numbers
- Provide a simple interface for asking questions such as address and eligibility queries

## Project overview

The project explores how document ingestion, embeddings, vector search and language-model generation fit together. It is a local prototype, not an official service or a substitute for advice. Answers can be incomplete or incorrect, so users should check the cited source documents.

The frontend is intentionally lightweight and designed for local testing and demos. The current indexing endpoint processes the PDFs included in `server/DocumentsToRead/`; it does not provide a general-purpose document upload flow.

## Repository structure

```text
AskTheDocs/
├── README.md
├── AskTheDocs.slnx
├── client/
│   ├── package.json
│   ├── src/
│   ├── public/
│   └── vite.config.ts
├── server/
│   ├── appsettings.json
│   ├── AskTheDocs.Api.csproj
│   ├── Program.cs
│   ├── Controllers/
│   ├── DocumentsToRead/
│   ├── Models/
│   ├── Services/
│   └── Exceptions/
└── AskTheDocs.Tests/
    └── ...
```

## Tech stack

- Frontend: React 19, Vite, TypeScript
- Backend: ASP.NET Core (.NET 10)
- AI / embeddings and answer generation: Ollama, using the configured local models
- Vector database: Qdrant
- PDF parsing: PdfPig
- Testing: xUnit

## How the components fit together

### Indexing documents

1. The API reads each built-in PDF and extracts its text page by page with PdfPig.
2. The text is split into chunks, retaining the source page number.
3. Ollama generates an embedding for each chunk.
4. The API stores each chunk's text, document name, page number and embedding in Qdrant.

### Asking a question

1. The React app sends the question to the ASP.NET Core API.
2. The API embeds the original question and searches Qdrant for relevant passages.
3. Ollama generates alternative query phrasings using the initial search results as context. The API embeds and searches those queries too, then deduplicates and ranks the combined results.
4. The API sends the question and retrieved passages to Ollama to generate an answer grounded in the supplied text.
5. The response includes the answer and source passages. Citations identify both the document and page, since page numbers alone are ambiguous across multiple PDFs.

For questions asking for an address, the API also tries a deterministic extraction from the retrieved passages and document's last page, rather than relying only on the language model to reproduce the address.

## What this project explores

Building the prototype highlighted that RAG quality depends on the whole pipeline, not just the language model:

- PDF text extraction and chunk boundaries affect what information can be found.
- Embeddings and vector search retrieve semantically related passages, but relevance still needs to be checked.
- Query expansion can add useful search variations, but those results still need deduplication and grounding in the source text.
- Prompts encourage grounded answers and citations, but do not guarantee correctness; source passages need to remain visible and verifiable.
- Citations need enough detail to locate the source unambiguously, including both document name and page.
- Exact structured information, such as a postal address, may benefit from deterministic extraction.

The existing xUnit tests cover individual services and pipeline building blocks. They do not constitute a complete evaluation of retrieval relevance or factual answer quality; building a representative evaluation set is a useful next step.

## Prerequisites

Before running the app locally, make sure you have:

- .NET 10 SDK
- Node.js 20+ and npm
- Docker (recommended for running Qdrant)
- Ollama installed and running locally

## Configuration

The backend configuration is in `server/appsettings.json`.

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434/",
    "EmbeddingModel": "nomic-embed-text",
    "GenerationModel": "qwen3.5:4b"
  },
  "Qdrant": {
    "Host": "localhost",
    "Port": 6334,
    "CollectionName": "ask-the-docs",
    "VectorSize": 768
  }
}
```

You will need the following Ollama models available locally:

```bash
ollama pull nomic-embed-text
ollama pull qwen3.5:4b
```

Qdrant should be listening on `localhost:6334`.

If you use Docker:

```bash
docker run -d --name qdrant -p 6334:6334 qdrant/qdrant
```

## Getting started

### 1) Clone the repository

```bash
git clone https://github.com/BarkermanX/AskTheDocs.git
cd AskTheDocs
```

### 2) Restore .NET dependencies

```bash
dotnet restore
```

### 3) Install frontend dependencies

```bash
cd client
npm install
cd ..
```

### 4) Start Ollama and Qdrant

Make sure Ollama is running, then pull the required models and start Qdrant:

```bash
ollama serve
ollama pull nomic-embed-text
ollama pull qwen3.5:4b
docker run -d --name qdrant -p 6334:6334 qdrant/qdrant
```

### 5) Run the backend

From the repository root:

```bash
dotnet run --project server/AskTheDocs.Api.csproj
```

The API will start using the configuration in `server/appsettings.json`.

### 6) Run the frontend

In a second terminal:

```bash
cd client
npm run dev
```

Then open the local Vite URL in your browser.

## Using the app

Start the backend and frontend, then index the built-in documents by calling `POST /api/documents/index`. Once indexing completes, you can ask questions such as:

- Where should I send my completed PIP form?
- What evidence is needed for a PIP claim?
- What are the assessment criteria?

The API exposes the following endpoints:

### Index documents

```http
POST /api/documents/index
```

### List indexed documents

```http
GET /api/documents
```

### Delete an indexed document

```http
DELETE /api/documents/{id}
```

### Ask a question

```http
POST /api/questions?question=Where%20should%20I%20send%20my%20completed%20PIP%20form%3F
```

Example response:

```json
{
  "question": "Where should I send my completed PIP form?",
  "answer": "Send the completed form to ...",
  "sources": [
    {
      "documentId": "pip2-form-and-information-booklet.pdf",
      "pageNumber": 12,
      "text": "...",
      "score": 0.92
    }
  ]
}
```

## Document sources

The backend uses the PDFs in `server/DocumentsToRead/` for indexing. These are example PIP guidance documents included in the project for local testing.

## Notes

- The API creates the configured Qdrant collection if it does not already exist. Qdrant must be available when indexing and asking questions.
- Indexing uses the fixed set of PDFs in `server/DocumentsToRead/`; adding support for arbitrary uploads and persisted document management is not part of this prototype.
- The app is designed for local research or demo use rather than production deployment.
- For best results, keep Ollama and Qdrant running while using the application.

## License

This project does not currently declare a license file. Add a license before publishing or distributing the code externally.

## Contributing

Possible next steps include:

- evaluating retrieval and answer quality against a representative question set
- improving chunking, query expansion or reranking
- adding document upload and persisted document management
- adding authentication and production-ready deployment safeguards
