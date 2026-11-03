# AskTheDocs

AskTheDocs is a document Q&A application built for reviewing and answering questions about Personal Independence Payment (PIP) guidance documents. It uses a retrieval-augmented generation (RAG) pipeline to search PDF source material, expand the user query, and return grounded answers with supporting source passages.

The application combines:
- A .NET 10 ASP.NET Core API for indexing and answer generation
- A React + Vite frontend for asking questions in a simple chat-style experience
- Ollama for embeddings and LLM generation
- Qdrant for vector search
- PDF extraction and chunking using PdfPig

## Features

- Upload and index PDF guidance documents from the built-in document set
- Convert PDF pages into searchable text chunks
- Generate embeddings for semantic retrieval
- Expand user questions to improve recall
- Return answers grounded in relevant document passages
- Surface document/page source references alongside each answer
- Provide a simple interface for asking questions such as address and eligibility queries

## Project overview

This repo contains a full-stack prototype for a PIP information assistant. The backend indexes a curated set of UK Government PIP documents and answers questions using retrieval over those documents rather than relying on generic model knowledge alone.

The frontend is intentionally lightweight and designed to be easy to run locally for testing and demos.

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
- AI / embeddings: Ollama
- Vector database: Qdrant
- PDF parsing: PdfPig
- Testing: xUnit

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

Once the backend is running and the documents have been indexed, you can ask questions such as:

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

- The API expects a Qdrant collection to exist and will create it automatically if it does not.
- The app is designed for local research or demo use rather than production deployment.
- For best results, keep Ollama and Qdrant running while using the application.

## License

This project does not currently declare a license file. Add a license before publishing or distributing the code externally.

## Contributing

If you want to extend the application, consider:

- adding new document sources
- improving query expansion or reranking
- adding authentication or secure deployments
- introducing a proper persisted document upload flow

