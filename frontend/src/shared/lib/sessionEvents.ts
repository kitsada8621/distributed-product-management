type Listener = () => void

const listeners = new Set<Listener>()

export function onSessionCleared(listener: Listener): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

export function emitSessionCleared() {
  listeners.forEach((listener) => listener())
}
