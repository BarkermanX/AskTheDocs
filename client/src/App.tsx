
import { useState } from 'react'
import './App.css'

type Source = {
  documentId: string
  pageNumber: number
  text: string
  score: number
}

type AskResponse = {
  question: string
  answer: string
  sources: Source[]
}

function App() {
  const [question, setQuestion] = useState('')
  const [response, setResponse] = useState<AskResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  async function askQuestion(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!question.trim()) return

    setLoading(true)
    setError('')
    setResponse(null)

    try {
      const params = new URLSearchParams({ question })

      const result = await fetch(`/api/questions?${params}`, {
        method: 'POST',
      })

      if (!result.ok) {
        throw new Error(await result.text() || 'The request failed.')
      }

      const data: AskResponse = await result.json()
      setResponse(data)
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Something went wrong while asking your question.',
      )
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="app">
      <header className="header">

        <h1>RAG Without Authorization</h1>
        <p>Try it yourself. Ask a PIP claim related question that accesses .gov documents stored on the server. The AI returns answers by using context from the official claim documentation and provides references</p>

        <h1>PIP Bot</h1>
        <p>Ask questions about your PIP claim</p>
        <p>Answers are sourced directly from the govenment documents with sources referenced.</p>
      </header>

      <form className="question-form" onSubmit={askQuestion}>
        <label htmlFor="question">Your question</label>

        <textarea
          id="question"
          value={question}
          onChange={(event) => setQuestion(event.target.value)}
          placeholder="e.g. Where should I send my completed PIP form?"
          rows={3}
        />

        <button type="submit" disabled={loading || !question.trim()}>
          {loading ? 'Finding your answer...' : 'Ask question'}
        </button>
      </form>

      {error && (
        <section className="error" role="alert">
          <h2>Unable to answer</h2>
          <p>{error}</p>
        </section>
      )}

      {response && (
        <section className="answer">
          <h2>Answer</h2>
          <p className="answer-text">{response.answer}</p>

          <h2>Sources</h2>

          {response.sources.length === 0 ? (
            <p>No source passages were returned.</p>
          ) : (
            <ul className="sources">
              {response.sources.map((source, index) => (
                <li key={`${source.documentId}-${source.pageNumber}-${index}`}>
                  <strong>{source.documentId}</strong>
                  <span>Page {source.pageNumber}</span>
                  <p>{source.text}</p>
                </li>
              ))}
            </ul>
          )}
        </section>
      )}
    </main>
  )
}

export default App
