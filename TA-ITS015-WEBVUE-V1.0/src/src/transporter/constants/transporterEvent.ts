// ─── 1. Tên event nội bộ FE (dùng trong mittBus.on / mittBus.off) ─────────────

export const TransporterEvent = {
    StatusFms: 'nats:status:fms',
    DataVms: 'nats:data:vms',
    DataIncident: 'nats:data:incident',
    DataFms: 'nats:data:fms',
    DataTmsZone: 'nats:data:tms:zone',
    VdsTrafficData: 'nats:data:vds:traffic',
    /** VideoWall: BE vừa kích hoạt một kịch bản lên tường (VwScene Active). */
    DataVideoWallScene: 'nats:data:videowall:scene',
    /** VideoWall: Service ITS.VideoWall phản hồi lệnh (VwCommandResponseEnvelope: MessageId, Action, Success, ErrorMessage, Data) + trạng thái heartbeat. */
    DataVideoWall: 'nats:data:videowall',
    VehicleAlertData: 'nats:data:vds:filter',
    /** Toll: kết quả lấy dữ liệu thu phí thủ công (Service phản hồi Type="result"). */
    TollControl: 'nats:fms:control:toll',
    /** Topology: kết quả lấy dữ liệu topology thủ công (DeviceMonitor phản hồi Type="result"). */
    TopologyControl: 'nats:control:topology',
    /** Noti: thông báo (notification) mới gửi tới người dùng hiện tại. */
    Noti: 'nats:data:noti',
    /** FMS: kết quả lệnh điều khiển FMS (DeviceMonitor phản hồi Type="result"), vd lấy topology. */
    FmsControl: 'nats:control:fms',
    /** Heartbeat giám sát dịch vụ: mọi gói ta.its.heartbeat.<phân hệ> (payload { subject, data, receivedAt }). */
    Heartbeat: 'nats:heartbeat',
    Refresh: 'nats:refresh',
} as const;

export type TransporterEventType = typeof TransporterEvent[keyof typeof TransporterEvent];


//#region NATS.IO

// ─── 1b. Tên subject NATS (set cứng, khớp với "subject" trong nats.subjects.json) ─
export const NatsSubject = {
    StatusFms: 'ta.its.status.fms',
    DataVms: 'ta.its.data.vms',
    DataIncident: 'ta.its.data.incident',
    DataFms: 'ta.its.data.fms',
    DataTmsZone: 'ta.its.data.tms.zone',
    VdsTrafficData: 'ta.its.data.vds.traffic',
    VehicleAlertData: 'ta.its.data.vds.filter',
    /**
     * VideoWall — BE publish sau khi `ActiveVwScene` chạy xong.
     * Payload tối thiểu: { Code, EventTypeId } (khớp VwActiveSceneInput); nếu BE gửi
     * cả VwActiveSceneOutput thì FE dùng thêm Id/SceneId/Name — xem hook
     * transporter/hooks/useTransporterVideoWallScene.ts.
     */
    DataVideoWallScene: 'ta.its.data.videowall.scene',
    /** VideoWall — Service ITS.VideoWall publish kết quả lệnh (ghép theo MessageId BE trả về) + trạng thái màn / nguồn (heartbeat). */
    DataVideoWall: 'ta.its.data.videowall',
    /**
     * Toll — kênh điều khiển lấy dữ liệu thu phí thủ công (dùng chung 2 chiều, phân biệt bằng field Type).
     * FE gửi REST -> BE publish Type="request" -> Service xử lý -> publish Type="result" cho FE.
     * FE chỉ quan tâm gói Type="result", đối chiếu theo RequestId (xem hooks/useManualFetchToll.ts).
     */
    TollControl: 'ta.its.control.toll',
    /** Topology — kênh điều khiển lấy dữ liệu thủ công 2 chiều (Type=request/result), FE chỉ dùng gói Type="result". */
    TopologyControl: 'ta.its.control.topology',
    /**
     * Noti — BE publish khi có thông báo mới gửi tới người dùng/tổ chức/toàn hệ thống.
     * Payload khớp NotiListOutput: { id, messageId, title, content, category, severity, sourceKey, sendTime }.
     */
    Noti: 'ta.its.noti',
    /** FMS — kênh điều khiển FMS dùng chung 2 chiều (Type=request/result, Action phân loại lệnh); FE chỉ dùng gói Type="result". */
    FmsControl: 'ta.its.control.fms',
    /**
     * Heartbeat giám sát dịch vụ — tiền tố dùng chung. Mỗi dịch vụ phát lên "ta.its.heartbeat.<phân hệ>"
     * (vd ta.its.heartbeat.toll, ta.its.heartbeat.fms). FE subscribe wildcard <see cref="HeartbeatWildcard"/>.
     */
    HeartbeatPrefix: 'ta.its.heartbeat',
    /** Wildcard đăng ký subscribe để bắt mọi heartbeat của các phân hệ (khai trong Nats.subjects.json). */
    HeartbeatWildcard: 'ta.its.heartbeat.>',
    Refresh: 'ta.its.refresh',
} as const;

export type NatsSubjectType = typeof NatsSubject[keyof typeof NatsSubject];

// ─── 2. Ánh xạ subject → event (set cứng, FE tự quản lý) ─────────────────────
// TANatsClient dùng map này trong handleSubjectData() để biết
// "nhận subject X thì emit event gì lên mittBus".
// Subject phải khớp với trường "subject" trong nats.subjects.json.
export const NatsSubjectToEvent: Record<string, TransporterEventType> = {
    [NatsSubject.StatusFms]: TransporterEvent.StatusFms,
    [NatsSubject.DataVms]: TransporterEvent.DataVms,
    [NatsSubject.DataIncident]: TransporterEvent.DataIncident,
    [NatsSubject.DataFms]: TransporterEvent.DataFms,
    [NatsSubject.DataTmsZone]: TransporterEvent.DataTmsZone,
    [NatsSubject.VdsTrafficData]: TransporterEvent.VdsTrafficData,
    [NatsSubject.VehicleAlertData]: TransporterEvent.VehicleAlertData,
    [NatsSubject.DataVideoWallScene]: TransporterEvent.DataVideoWallScene,
    [NatsSubject.DataVideoWall]: TransporterEvent.DataVideoWall,
    [NatsSubject.TollControl]: TransporterEvent.TollControl,
    [NatsSubject.TopologyControl]: TransporterEvent.TopologyControl,
    [NatsSubject.Noti]: TransporterEvent.Noti,
    [NatsSubject.FmsControl]: TransporterEvent.FmsControl,
    [NatsSubject.Refresh]: TransporterEvent.Refresh,
};

//#endregion
