import socketCluster from 'socketcluster-client'; // pnpm install socketcluster-client
import { onMounted, onUnmounted } from 'vue';
import { useThemeVms } from '../stores/its/themeVms';

// Begin- Add by DangKhoa
import { useMonitorVms } from '../stores/its/monitorVms';
// End- Add by DangKhoa
// Begin - Add by Ngọc Thắng
import { useIncidentMonitor } from '../stores/its/incidentMonitor';
// End - Add by Ngọc Thắng

interface ServerConfig {
  hostname: string;
  port: number;
  secure: boolean;
}

class TransporterSocketCluster {
  private socket: socketCluster;
  private readonly config: ServerConfig;
  private themeStore = useThemeVms(); // Instantiate the Pinia store

  // Begin - Add by Dang Khoa
  private monitorVms = useMonitorVms(); // Instantiate the Pinia store
  // End - Add by Dang Khoa
  // Begin - Add by Ngọc Thắng
  private incidentMonitor = useIncidentMonitor();
  // End - Add by Ngọc Thắng

  constructor(config: ServerConfig) {
    this.config = config;
    this.socket = socketCluster.create({
      hostname: config.hostname,
      port: config.port,
      secure: config.secure ?? false,
      autoReconnect: true, // Enable auto-reconnect
      autoReconnectOptions: {
        initialDelay: 1000, // Initial reconnect delay in milliseconds
        randomness: 1000,   // Randomness in reconnect delay in milliseconds
        multiplier: 1.5,    // Multiplier for increasing delay
        maxDelay: 30000,    // Maximum reconnect delay in milliseconds
      },
      wsOptions: { autoPong: false }
    });
    this.init();
  }

  // Initialize socket
  private async init(): Promise<void> {
    try {
      await this.socket.connect();
      console.log('Connecting to SocketCluster server');
      // this.socket.on('connect', () => {
      //   console.log('Connected to SocketCluster server');
      // });
      this.subscribeChannel();
    } catch (error) {
      console.error('Error connecting to SocketCluster server:', error);
    }
  }

  // Subscribe to channel - Receive data from server
  private subscribeToChannel(channelName: string): void {
    const channel = this.socket.subscribe(channelName);

    (async () => {
      for await (const data of channel) {
        this.handleChannelData(channelName, data);
      }
    })();
  }

  // Publish to channel - Send data to server
  public async publishToChannel(channelName: string, data: unknown): Promise<void> {
    try {
      await this.socket.publish(channelName, data);
      console.log(`Published to ${channelName}:`, data);
    } catch (error) {
      console.error(`Error publishing to ${channelName}:`, error);
    }
  }

  // Disconnect socket
  public disconnect(): void {
    this.socket.disconnect();
    console.log('Disconnected from SocketCluster server');
  }

  // Subscribe to channels
  private subscribeChannel(): void {
    // Add channels to subscribe to
    this.subscribeToChannel('ta.test');
    this.subscribeToChannel('vms.update');

    // Begin - Add by Ngọc Thắng
    this.subscribeToChannel('its.incident');
    this.subscribeToChannel('its.workExecute');
    // End - Add by Ngọc Thắng
  }

  // Handle channel data
  private handleChannelData(channelName: string, data: any): void {
    // Handle the received data here
    switch (channelName) {
      case 'ta.test':
        // Here you might want to update the store if the data format allows it
        // For example, if data is an object containing theme configuration
        this.themeStore.setThemeConfig(data); // Update the store with new data
        break;

      case 'vms.update':
        //console.log("Test received socket", data)
        //console.log("Test received socket - channelName", channelName)
        this.monitorVms.url = data.Data.Url;
        this.monitorVms.eqipmentId = data.Data.EquipmentId;
        this.monitorVms.isVmsUpdate = !this.monitorVms.isVmsUpdate;
        break;

      // Begin - Add by Ngọc Thắng
      case 'its.incident':
        if (data?.broadcastCode != this.incidentMonitor.broadcastCode) {
          this.incidentMonitor.broadcastCode = data?.broadcastCode;
          this.incidentMonitor.broadcastObject = data?.broadcastObject;
          this.incidentMonitor.broadcastDateTime = data?.broadcastDateTime;
          this.incidentMonitor.broadcastMessage = data?.broadcastMessage;
        }

        break;
      // End - Add by Ngọc Thắng

      default:
        break;
    }
  }

  // onMounted lifecycle hook
  onMounted(): void {
    this.init(); // Initialize the socket on mount
  }

  // onUnmounted lifecycle hook
  onUnmounted(): void {
    this.disconnect(); // Disconnect the socket on unmount
  }
}

export default TransporterSocketCluster;
