import { useEffect, useState } from 'react'
import './App.css'

function App() {
  const [count, setCount] = useState(0)

  useEffect(() => {
    const ws = new WebSocket('ws://localhost:8222/sub/', 'nemcache-0.1')
    ws.onopen = () => {
      console.log('Connection open!')
      ws.send(JSON.stringify({ command: 'subscribe', key: 'click' }))
    }
    ws.onclose = () => {
      console.log('Connection closed')
    }
    ws.onerror = (error) => {
      console.log('Error detected:', error)
    }
    ws.onmessage = (event) => {
      console.log(event.data)
      const o = JSON.parse(event.data)
      alert(o.data)
    }
    return () => {
      ws.close()
    }
  }, [])

  const onClick = async () => {
    const newCount = count + 1
    setCount(newCount)
    await fetch('http://localhost:8222/cache/click', {
      method: 'PUT',
      body: 'Clicked: ' + newCount,
    })
  }

  return (
    <div className="card">
      <button onClick={onClick}>Click ({count})</button>
    </div>
  )
}

export default App
