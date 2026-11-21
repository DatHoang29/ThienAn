/**
 * transporterNats.ts
 * Dùng NatsSubjectToEvent từ transporterEvent.ts để ánh xạ subject → event,
 * thay vì đọc trường "event" từ JSON.
 */

import { connect, JSONCodec, NatsConnection, Subscription, ConnectionOptions, credsAuthenticator, } from 'nats.ws';
import mittBus from '/@/utils/mitt';
import { ElMessage } from 'element-plus';
import { loadNatsSubjectConfig, getNatsSubjectConfigMap, type NatsSubjectConfigMap, } from './constants/natsSubject';
// NatsSubjectToEvent: subject → event name (set cứng trong FE)
import { NatsSubjectToEvent, NatsSubject, TransporterEvent } from './constants/transporterEvent';
import { useMonitorVms } from '../stores/its/monitorVms';
import { getVmsEquipmentId, normalizeVmsPayload } from './hooks/useTransporterVms';
import { useMapStore } from '../stores/its/mapStore';

// ─── Kiểu dữ liệu khớp với nats.config.json ──────────────────────────────────
interface NatsConfigJson {
  Nats: {
    Url: string;
    ClientName?: string;
    AuthMode?: 'JWT' | 'Token' | 'None';
    CredsFile?: string;
    Token?: string;
    AutoSubscribe?: boolean;
    MaxReconnectAttempts?: number;
    ReconnectTimeWaitMs?: number;
  };
}

export interface TANatsClientOptions {
  servers?: string;
  name?: string;
  token?: string;
  creds?: string;
  autoSubscribe?: boolean;
  maxReconnectAttempts?: number;
  reconnectTimeWait?: number;
}

// ─── Helpers cho creds ────────────────────────────────────────────────────────

/**
 * Regex tách khối JWT/SEED, COPY nguyên trạng từ nats.ws@1.30.3
 * (lib/nats-base-client/authenticator.js). Dùng để tự kiểm tra trước khi giao
 * cho credsAuthenticator, vì credsAuthenticator parse LAZY — nó chỉ chạy regex
 * lúc connect() gọi authenticator(nonce), nên nếu để nó tự ném thì lỗi trả về
 * chỉ là "BAD_CREDS" trần, không biết file sai ở đâu.
 */
const CREDS_BLOCK_RE = /\s*(?:(?:[-]{3,}[^\n]*[-]{3,}\n)(.+)(?:\n\s*[-]{3,}[^\n]*[-]{3,}\n))/gi;

/**
 * Chuẩn hoá nội dung file .creds trước khi đưa cho credsAuthenticator.
 *
 * NGUYÊN NHÂN GỐC của lỗi "Lỗi kết nối NATS": regex ở trên yêu cầu NGAY SAU dãy
 * dấu '-' phải là '\n'. File nats.creds lưu bằng editor Windows (hoặc checkout
 * qua git core.autocrlf=true) có kết thúc dòng CRLF, tức '\r' chen vào giữa dãy
 * '-' và '\n' => regex KHÔNG khớp => credsAuthenticator ném BAD_CREDS =>
 * connect() thất bại dù URL, port và creds đều hợp lệ.
 *
 * Vì file này sẽ còn bị các editor/deploy tool trên Windows đổi lại thành CRLF,
 * việc chuẩn hoá phải làm ở code chứ không thể chỉ sửa file một lần.
 */
function normalizeCredsText(text: string): string {
  // Bỏ BOM nếu file được lưu dạng UTF-8 with BOM, rồi đưa CRLF/CR về LF.
  return text.replace(/^\uFEFF/, '').replace(/\r\n?/g, '\n');
}

/**
 * Kiểm tra file creds có đủ 2 khối (JWT + NKEY SEED) mà nats.ws đọc được không.
 * Trả về thông báo lỗi cụ thể, hoặc null nếu hợp lệ.
 */
function validateCredsText(text: string): string | null {
  CREDS_BLOCK_RE.lastIndex = 0;
  const blocks = text.match(CREDS_BLOCK_RE);
  if (!blocks || blocks.length < 2) {
    return `nats.creds sai định dạng: cần đủ 2 khối "BEGIN NATS USER JWT" và "BEGIN USER NKEY SEED" (tìm thấy ${blocks?.length ?? 0})`;
  }
  return null;
}

/**
 * Chuẩn hóa URL sang WebSocket (ws:// hoặc wss://)
 * Hỗ trợ tự động chuyển https:// -> wss://, http:// -> ws://,
 * và nâng cấp lên wss:// nếu trang đang chạy HTTPS.
 */
function normalizeWebSocketUrl(rawUrl: string): string {
  let url = rawUrl.trim();
  if (url.startsWith('https://')) {
    url = 'wss://' + url.slice('https://'.length);
  } else if (url.startsWith('http://')) {
    url = 'ws://' + url.slice('http://'.length);
  } else if (url.startsWith('tls://')) {
    url = 'wss://' + url.slice('tls://'.length);
  }

  // Nếu trình duyệt đang chạy dưới HTTPS, WebSocket BẮT BUỘC phải là wss://
  if (typeof window !== 'undefined' && window.location?.protocol === 'https:' && url.startsWith('ws://')) {
    url = 'wss://' + url.slice('ws://'.length);
  }

  return url;
}

// ─── Thông báo trạng thái kết nối ─────────────────────────────────────────────
// Chỉ hiển thị thông báo CHUNG khi kết nối / mất kết nối (cùng kiểu với thông báo thực hiện thành công).
// Mọi lỗi (cấu hình, creds, connect, publish, subscribe...) CHỈ ghi log console, KHÔNG hiển thị lên FE.
const NatsStatusMessage = {
  connected: 'Kết nối máy chủ thành công',
  disconnected: 'Mất kết nối máy chủ',
} as const;

// ─── Class chính ──────────────────────────────────────────────────────────────

class TANatsClient {
  // #region Fields & Constructor
  private nc: NatsConnection | null = null;
  private readonly jc = JSONCodec<any>();
  private readonly subscriptions: Subscription[] = [];
  private isConnecting = false;
  // Đang ở trạng thái mất kết nối: tránh lặp thông báo "mất kết nối" và chỉ báo "kết nối" khi thật sự nối lại.
  private isOffline = false;
  private subjectMap: NatsSubjectConfigMap = {};
  private readonly customOptions?: TANatsClientOptions;

  constructor(options?: TANatsClientOptions) {
    this.customOptions = options;
    this.init();
  }
  // #endregion

  // #region Init & Connection
  private async init(): Promise<void> {
    try {
      this.subjectMap = await loadNatsSubjectConfig();
      await this.connect();
    } catch (error) {
      // Lỗi chỉ ghi log, không hiển thị lên FE
      console.error('[NATS] Init error:', error);
    }
  }

  public async connect(): Promise<void> {
    if (this.nc || this.isConnecting) return;
    this.isConnecting = true;

    // Mô tả chặng đang chạy, để khi lỗi biết CHÍNH XÁC bước nào fail.
    let step = 'đọc cấu hình NATS';

    try {
      let natsConfig: NatsConfigJson['Nats'] | null = null;

      try {
        const cfgRes = await fetch('Configuration/Nats.config.json');
        if (cfgRes.ok) {
          const cfg: NatsConfigJson = await cfgRes.json();
          natsConfig = cfg?.Nats;
        }
      } catch (e) {
        console.warn('[NATS] Không thể tải Configuration/Nats.config.json, sử dụng cấu hình truyền vào/env:', e);
      }

      // Thứ tự ưu tiên URL:
      // 1. customOptions.servers (truyền trực tiếp từ new TANatsClient(...))
      // 2. natsConfig.Url (từ Configuration/Nats.config.json)
      // 3. window.__env__?.VITE_NATS_WS_URL (runtime config)
      // 4. import.meta.env.VITE_NATS_WS_URL (build-time Vite env)
      const rawUrl =
        this.customOptions?.servers ||
        natsConfig?.Url ||
        (typeof window !== 'undefined' && (window as any).__env__?.VITE_NATS_WS_URL) ||
        (import.meta as any).env?.VITE_NATS_WS_URL;

      if (!rawUrl) {
        throw new Error('thiếu cấu hình NATS WebSocket URL (Nats.Url hoặc VITE_NATS_WS_URL)');
      }

      const targetUrl = normalizeWebSocketUrl(rawUrl);

      // Trang https không được mở WebSocket ws:// (mixed content bị browser chặn)
      if (typeof window !== 'undefined' && window.location?.protocol === 'https:' && targetUrl.startsWith('ws://')) {
        throw new Error(`trang đang chạy HTTPS nên browser chặn ${targetUrl}, phải đổi sang wss://`);
      }

      const clientName = this.customOptions?.name ?? natsConfig?.ClientName ?? 'ITS_FE_WEB_CLIENT';
      const maxReconnectAttempts = this.customOptions?.maxReconnectAttempts ?? natsConfig?.MaxReconnectAttempts ?? -1;
      const reconnectTimeWait = this.customOptions?.reconnectTimeWait ?? natsConfig?.ReconnectTimeWaitMs ?? 1000;
      const autoSubscribe = this.customOptions?.autoSubscribe ?? natsConfig?.AutoSubscribe ?? true;

      const options: ConnectionOptions = {
        servers: targetUrl,
        name: clientName,
        maxReconnectAttempts,
        reconnectTimeWait,
      };

      // Xử lý xác thực: Ưu tiên creds truyền trực tiếp -> CredsFile -> Token
      if (this.customOptions?.creds) {
        step = 'phân tích creds được truyền vào';
        const credsText = normalizeCredsText(this.customOptions.creds);
        const credsError = validateCredsText(credsText);
        if (credsError) throw new Error(credsError);
        options.authenticator = credsAuthenticator(new TextEncoder().encode(credsText));
      } else if (natsConfig?.AuthMode === 'JWT' && natsConfig?.CredsFile) {
        console.log('[NATS] Using JWT creds. File:', natsConfig.CredsFile);
        step = `đọc creds "${natsConfig.CredsFile}"`;
        const credsRes = await fetch(natsConfig.CredsFile);
        if (!credsRes.ok) {
          throw new Error(`HTTP ${credsRes.status} ${credsRes.statusText} — kiểm tra file có trong dist/ và web server cho phép phục vụ đuôi .creds`);
        }

        step = `phân tích creds "${natsConfig.CredsFile}"`;
        const credsText = normalizeCredsText(await credsRes.text());
        const credsError = validateCredsText(credsText);
        if (credsError) throw new Error(credsError);

        options.authenticator = credsAuthenticator(new TextEncoder().encode(credsText));
      } else if (this.customOptions?.token || (natsConfig?.AuthMode === 'Token' && natsConfig?.Token)) {
        console.log('[NATS] Using token authenticator');
        options.token = this.customOptions?.token || natsConfig?.Token;
      } else {
        console.log('[NATS] Connecting without authenticator');
      }

      step = `kết nối ${targetUrl}`;
      console.log(`[NATS] Đang kết nối tới ${targetUrl}...`);
      this.nc = await connect(options);
      console.log('[NATS] Connected:', this.nc.getServer());
      this.isOffline = false;
      ElMessage.success(NatsStatusMessage.connected);
      mittBus.emit('event-nats-status' as any, { type: 'connect', server: this.nc.getServer() });

      this.watchConnectionStatus();

      if (autoSubscribe) {
        this.subscribeSubjects();
      }
    } catch (error) {
      const detail = (error as any)?.code || (error as Error)?.message || String(error);

      // Nếu kết nối qua wss:// gặp lỗi (đặc biệt là chứng chỉ tự ký trên trình duyệt)
      let advice = '';
      if (step.includes('wss://')) {
        advice = ' (Lưu ý: Nếu server wss dùng chứng chỉ TLS tự ký/không hợp lệ, vui lòng mở URL wss/https trên tab trình duyệt để Proceed/Trust chứng chỉ)';
      }
      // Lỗi chỉ ghi log (giữ đủ chi tiết bước lỗi để chẩn đoán), không hiển thị lên FE
      console.error(`[NATS] Lỗi kết nối NATS khi ${step}: ${detail}${advice}`, error);
    } finally {
      this.isConnecting = false;
    }
  }

  private async watchConnectionStatus(): Promise<void> {
    if (!this.nc) return;
    try {
      for await (const status of this.nc.status()) {
        console.log('[NATS] Status:', status);
        mittBus.emit('event-nats-status' as any, status);
        switch (status.type) {
          case 'disconnect':
            // Chỉ báo 1 lần cho mỗi lần mất kết nối (không lặp trong lúc thư viện tự reconnect)
            if (!this.isOffline) {
              this.isOffline = true;
              ElMessage.warning(NatsStatusMessage.disconnected);
            }
            break;
          case 'reconnect':
            if (this.isOffline) {
              this.isOffline = false;
              ElMessage.success(NatsStatusMessage.connected);
            }
            break;
          case 'error':
            console.error('[NATS] Status error:', status);
            break;
        }
      }
    } catch (error) {
      console.error('[NATS] Watch status error:', error);
    }
  }

  public async disconnect(): Promise<void> {
    try {
      this.unsubscribeAll();
      if (this.nc) {
        await this.nc.drain();
        this.nc = null;
      }
      // Ngắt chủ động (unmount App) không phải mất kết nối -> chỉ ghi log, không hiển thị thông báo
      console.log('[NATS] Disconnected');
    } catch (error) {
      console.error('[NATS] Disconnect error:', error);
    }
  }
  // #endregion

  // #region Publish
  public publishToSubject(subject: string, data: unknown): void {
    try {
      // Lỗi chỉ ghi log, không hiển thị lên FE
      if (!this.nc) { console.error(`[NATS] Publish bỏ qua: chưa kết nối. Subject=${subject}`); return; }
      if (!subject) { console.error('[NATS] Publish bỏ qua: subject trống'); return; }
      this.nc.publish(subject, this.jc.encode(data));
      console.log(`[NATS] Published to ${subject}:`, data);
    } catch (error) {
      console.error(`[NATS] Publish error. Subject=${subject}`, error);
    }
  }
  // #endregion

  // #region Subscribe
  private subscribeSubjects(): void {
    Object.values(this.subjectMap).forEach(cfg => {
      if (cfg.allowSubscribeOnFe) {
        this.subscribeToSubject(cfg.subject);
      }
    });
  }

  public subscribeToSubject(subject: string): void {
    if (!this.nc) {
      console.warn(`[NATS] Cannot subscribe — connection null. Subject=${subject}`);
      return;
    }
    if (!subject) { console.warn('[NATS] Subject is empty'); return; }

    const sub = this.nc.subscribe(subject);
    this.subscriptions.push(sub);
    console.log(`[NATS] Subscribed: ${subject}`);

    (async () => {
      try {
        for await (const msg of sub) {
          const data = this.decodeMessage(msg.data);
          // Dùng msg.subject (subject THỰC của gói) thay vì subject đã đăng ký, để hỗ trợ wildcard
          // (vd đăng ký "ta.its.heartbeat.>" nhưng gói đến là "ta.its.heartbeat.toll").
          this.handleSubjectData(msg.subject ?? subject, data);
        }
      } catch (error) {
        console.error(`[NATS] Subscribe loop error. Subject=${subject}`, error);
      }
    })();
  }

  private decodeMessage(data: Uint8Array): any {
    try {
      return this.jc.decode(data);
    } catch (error) {
      console.error('[NATS] Decode message error:', error);
      return null;
    }
  }
  // #endregion

  // #region Subject Handlers
  private handleSubjectData(subject: string, data: any): void {
    if (data == null || data === '') return;

    // Heartbeat: subject cụ thể (ta.its.heartbeat.<phân hệ>) khớp wildcard đã đăng ký nhưng KHÔNG nằm
    // trong subjectMap. Xử lý riêng TRƯỚC bước kiểm tra map để trang giám sát dịch vụ nhận được mọi gói.
    if (subject === NatsSubject.HeartbeatPrefix || subject.startsWith(NatsSubject.HeartbeatPrefix + '.')) {
      this.handleHeartbeat(subject, data);
      return;
    }

    // Bước 1: kiểm tra subject có trong danh sách config không
    const map = Object.keys(this.subjectMap).length > 0
      ? this.subjectMap
      : (getNatsSubjectConfigMap() ?? {});

    if (!map[subject]) {
      console.warn(`[NATS] Subject không có trong config: ${subject}`);
      return;
    }

    // Bước 2: điều hướng theo subject → hàm xử lý riêng cho từng subject
    switch (subject) {
      case NatsSubject.DataIncident:
        this.handleIncidentData(subject, data);
        break;

      case NatsSubject.DataVms:
        this.handleVmsData(subject, data);
        break;

      case NatsSubject.StatusFms:
        this.handleFmsStatus(subject, data);
        break;

      case NatsSubject.DataFms:
        this.handleFmsData(subject, data);
        break;

      case NatsSubject.DataVideoWallScene:
        this.handleVideoWallScene(subject, data);
        break;

      // case NatsSubject.Noti:
      //   this.handleNotiData(subject, data);
      //   break;

      case NatsSubject.Refresh:
        this.handleRefresh(subject, data);
        break;

      default:
        // Subject có trong config nhưng chưa có handler riêng → chỉ emit event
        this.emitBySubject(subject, data);
        break;
    }
  }

  // ─── Handlers riêng cho từng subject ─────────────────────────────────────────

  // #region Per-subject Handlers
  /**
   * Description: Xử lý subject sự cố (incident) — emit event lên mittBus để
   *              component/store tự lắng nghe và xử lý (không update store trực tiếp).
   */
  private handleIncidentData(subject: string, data: any): void {
    // Log chặng "NATS → FE" của sự cố: tóm tắt để đối chiếu nhanh với form test FA/VDS, kèm payload đầy đủ.
    const d = typeof data === 'string' ? (() => { try { return JSON.parse(data); } catch { return data; } })() : data;
    console.groupCollapsed(`%c[NATS][Incident] ⇩ ${subject} | ${d?.changeType ?? '-'} | ${d?.code ?? d?.broadcastCode ?? '-'}`, 'color:#E6A23C;font-weight:bold');
    console.log('Tóm tắt:', {
      incidentId: d?.incidentId, code: d?.code, name: d?.name,
      viTri: d?.km != null ? `Km${d.km}+${d.met ?? 0}` : undefined,
      eventType: d?.eventTypeCode ?? d?.eventTypeId, eventGroup: d?.eventGroup, state: d?.state,
      zoneId: d?.zoneId, equipmentId: d?.equipmentId,
      broadcastObject: d?.broadcastObject, at: new Date().toLocaleTimeString(),
    });
    console.log('Payload:', d);
    console.groupEnd();
    this.emitBySubject(subject, data);
  }

  /**
   * Description: Xử lý subject VMS.
   */
  private handleVmsData(subject: string, data: any): void {
    const payload = normalizeVmsPayload(data);

    // Bước 4: cập nhật vmsCurrentList tại chỗ (không gọi lại API)
    const mapStore = useMapStore();
    mapStore.updateVmsCurrentFromNats(payload);

    // Toggle để LeafletMap vẽ lại marker
    const store = useMonitorVms();
    store.url = payload.data?.Url ?? payload.data?.url ?? null;
    store.eqipmentId = getVmsEquipmentId(payload.data) ?? null;
    store.isVmsUpdate = !store.isVmsUpdate;

    this.emitBySubject(subject, data);
  }

  /**
   * Description: Xử lý subject trạng thái thiết bị FMS.
   */
  private handleFmsStatus(subject: string, data: any): void {
    this.emitBySubject(subject, data);
  }

  /**
   * Description: Xử lý subject dữ liệu FMS.
   */
  private handleFmsData(subject: string, data: any): void {
    this.emitBySubject(subject, data);
  }

  /**
   * Description: Xử lý subject kích hoạt kịch bản VideoWall (BE `ActiveVwScene`).
   *              Chỉ emit lên mittBus; trang Monitor tự tra kịch bản và chuyển
   *              (xem hooks/useTransporterVideoWallScene.ts).
   */
  private handleVideoWallScene(subject: string, data: any): void {
    // Log chặng "NATS → FE": nếu KHÔNG thấy dòng này khi gọi POST /vwscene/active thì
    // vấn đề nằm ở phía BE hoặc ở subscribe, không phải ở hook/trang Monitor.
    console.log(`[NATS] ⇩ ${subject}`, data);
    this.emitBySubject(subject, data);
  }

  /**
   * Description: Xử lý subject refresh.
   */
  private handleRefresh(subject: string, data: any): void {
    this.emitBySubject(subject, data);
  }

  /**
   * Description: Xử lý heartbeat giám sát dịch vụ. Không tra NatsSubjectToEvent (subject là động theo phân hệ),
   *              mà emit thẳng 1 event dùng chung kèm subject thực + thời điểm nhận để trang dashboard tính Online/Offline.
   */
  private handleHeartbeat(subject: string, data: any): void {
    // Log chặng "NATS → FE": thấy dòng này nghĩa là dịch vụ ĐÃ phát và client ĐÃ nhận được gói.
    // Không thấy => hoặc dịch vụ chưa phát, hoặc client chưa subscribe wildcard ta.its.heartbeat.>
    console.log(`%c[Heartbeat] ⇩ ${subject}`, 'color:#67C23A;font-weight:bold', { data, at: new Date().toLocaleTimeString() });
    mittBus.emit(TransporterEvent.Heartbeat as any, { subject, data, receivedAt: Date.now() });
  }

  /**
   * Description: Xử lý subject thông báo (noti) — BE publish khi có noti mới.
   *              Chỉ emit lên mittBus; store notification (stores/its/notification.ts)
   *              tự lắng nghe để cập nhật badge/đếm chưa đọc và đẩy danh sách mới.
   */
  private handleNotiData(subject: string, data: any): void {
    // Log chặng "NATS → FE": KHÔNG thấy dòng này khi BE gửi noti nghĩa là BE chưa publish
    // đúng subject ta.its.noti hoặc server từ chối subscription (quyền nằm ở account JWT),
    // không phải lỗi ở hook/store phía FE.
    console.log(`[NATS] ⇩ ${subject}`, data);
    this.emitBySubject(subject, data);
  }

  /**
   * Description: Tra cứu event name từ NatsSubjectToEvent (set cứng phía FE)
   *              và emit dữ liệu lên mittBus.
   */
  private emitBySubject(subject: string, data: any): void {
    const eventName = NatsSubjectToEvent[subject];
    if (!eventName) {
      console.warn(`[NATS] Subject chưa có ánh xạ event trong NatsSubjectToEvent: ${subject}`);
      return;
    }

    // Log chặng "FE → mittBus": thấy dòng này mà component không phản ứng thì lỗi ở
    // phía hook/trang (chưa mount, đã unmount, hoặc lắng nghe sai tên event).
    console.log(`[NATS] → mittBus.emit('${eventName}')`);
    mittBus.emit(eventName as any, data);
  }
  // #endregion
  // #endregion

  // #region Cleanup & Lifecycle
  private unsubscribeAll(): void {
    for (const sub of this.subscriptions) {
      try { sub.unsubscribe(); } catch { /* ignore */ }
    }
    this.subscriptions.length = 0;
  }

  public async onMounted(): Promise<void> { await this.connect(); }
  public async onUnmounted(): Promise<void> { await this.disconnect(); }
  // #endregion
}

export default TANatsClient;