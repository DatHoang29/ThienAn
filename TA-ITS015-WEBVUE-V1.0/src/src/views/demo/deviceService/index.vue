<template>
  <div class="device-service-container">
    <div class="config-panel">
      <div class="form-item">
        <label>IP Address:</label>
        <input v-model="ipAddress" type="text" placeholder="Enter IP address" :disabled="isConnected" />
      </div>
      <div class="form-item">
        <label>Port:</label>
        <input v-model="port" type="text" placeholder="Enter port" :disabled="isConnected" />
      </div>
      <div class="form-item">
        <label>Interval (ms):</label>
        <input v-model.number="intervalTime" type="number" placeholder="Enter interval time" :disabled="isConnected" />
      </div>
      <button @click="toggleConnection" :class="{ 'connected': isConnected }">
        {{ isConnected ? 'Disconnect' : 'Connect' }}
      </button>
    </div>

    <div class="data-panels">
      <div class="panel api-panel">
        <h3>API Data</h3>
        <input v-model="apiData" type="text" readonly placeholder="API data will appear here..." />
        <div class="status">Last updated: {{ lastApiUpdate }}</div>
      </div>

      <div class="panel socket-panel">
        <h3>WebSocket Data</h3>
        <input v-model="socketData" type="text" readonly placeholder="WebSocket data will appear here..." />
        <div class="status">Connection status: {{ socketStatus }}</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onBeforeUnmount } from 'vue';

// State
const ipAddress = ref('127.0.0.1');
const port = ref('8080');
const intervalTime = ref(2000); // Default interval time
const isConnected = ref(false);
const apiData = ref('');
const socketData = ref('');
const lastApiUpdate = ref('Not fetched yet');
const socketStatus = ref('Disconnected');

// API polling interval
let apiInterval = null;

// WebSocket connection
let socket = null;

// Toggle connection status
const toggleConnection = () => {
  if (isConnected.value) {
    disconnect();
  } else {
    connect();
  }
};

// Connect to both API and WebSocket
const connect = () => {
  if (!ipAddress.value || !port.value) {
    alert('Please enter both IP address and port');
    return;
  }

  // Start API polling
  startApiPolling();
  
  // Connect WebSocket
  connectWebSocket();
  
  isConnected.value = true;
};

// Disconnect from both API and WebSocket
const disconnect = () => {
  // Stop API polling
  clearInterval(apiInterval);
  apiInterval = null;
  
  // Close WebSocket
  if (socket) {
    socket.close();
    socket = null;
  }
  
  socketStatus.value = 'Disconnected';
  isConnected.value = false;
};

// Start API polling
const startApiPolling = () => {
  // Clear any existing interval
  if (apiInterval) {
    clearInterval(apiInterval);
  }
  
  // Set up new polling interval
  apiInterval = setInterval(fetchApiData, intervalTime.value);
  
  // Initial fetch
  fetchApiData();
};



// Fetch data from API
const fetchApiData = async () => {
  try {
    const response = await fetch(`http://${ipAddress.value}:${port.value}/api/getdata`);
    if (!response.ok) {
      throw new Error(`HTTP error: ${response.status}`);
    }
    
    const data = await response.text();
    apiData.value = data;
    lastApiUpdate.value = new Date().toLocaleTimeString();
  } catch (error) {
    apiData.value = `Error: ${error.message}`;
    lastApiUpdate.value = `Error at ${new Date().toLocaleTimeString()}`;
    console.error('API fetch error:', error);
  }
};

// Connect to WebSocket
const connectWebSocket = () => {
  // Close existing socket if any
  if (socket) {
    socket.close();
  }
  
  // Create new WebSocket connection
  const wsUrl = `ws://${ipAddress.value}:${port.value}/ws`;
  socket = new WebSocket(wsUrl);
  
  // WebSocket event handlers
  socket.onopen = () => {
    socketStatus.value = 'Connected';
    console.log('WebSocket connected');
  };
  
  socket.onmessage = (event) => {
    socketData.value = event.data;
    console.log('WebSocket message received:', event.data);
  };
  
  socket.onerror = (error) => {
    socketStatus.value = 'Error';
    console.error('WebSocket error:', error);
  };
  
  socket.onclose = () => {
    socketStatus.value = 'Disconnected';
    console.log('WebSocket disconnected');
    // Attempt to reconnect after a delay
    if (isConnected.value) {
      setTimeout(connectWebSocket, intervalTime.value);
    }
  };
};

// Clean up on component unmount
onBeforeUnmount(() => {
  if (isConnected.value) {
    disconnect();
  }
});
</script>

<style scoped>
.device-service-container {
  padding: 20px;
  max-width: 1000px;
  margin: 0 auto;
  font-family: Arial, sans-serif;
}

.config-panel {
  display: flex;
  gap: 15px;
  margin-bottom: 20px;
  padding: 15px;
  background-color: #f5f5f5;
  border-radius: 5px;
  align-items: center;
}

.form-item {
  display: flex;
  flex-direction: column;
  min-width: 200px;
}

.form-item label {
  margin-bottom: 5px;
  font-weight: bold;
}

.form-item input {
  padding: 8px;
  border: 1px solid #ccc;
  border-radius: 4px;
}

button {
  padding: 8px 16px;
  background-color: #4CAF50;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  height: 36px;
  align-self: flex-end;
}

button:hover {
  background-color: #45a049;
}

button.connected {
  background-color: #f44336;
}

button.connected:hover {
  background-color: #d32f2f;
}

.data-panels {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
}

.panel {
  padding: 15px;
  background-color: #f9f9f9;
  border-radius: 5px;
  border: 1px solid #ddd;
}

.panel h3 {
  margin-top: 0;
  margin-bottom: 15px;
  color: #333;
}

.panel input {
  width: 100%;
  padding: 10px;
  margin-bottom: 10px;
  border: 1px solid #ccc;
  border-radius: 4px;
  font-family: monospace;
}

.status {
  font-size: 12px;
  color: #666;
}
</style>
