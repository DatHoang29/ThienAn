import { getAPI } from '/@/utils/axios-utils';
import { TmsZoneStatusApi, TmsVehicleFilterHistoryApi } from '/@/api-services/tms';
import type { VehicleFilterHistoryWindowInput, VehicleFilterHistoryWindowOutput, VehicleFilterHistoryTripInput, VehicleFilterHistoryTripOutput } from '/@/api-services/tms';

export type VehicleFilterHistoryInput = VehicleFilterHistoryWindowInput;
export type VehicleFilterHistoryOutput = VehicleFilterHistoryWindowOutput;
export type VehicleFilterTripInput = VehicleFilterHistoryTripInput;
export type VehicleFilterTripOutput = VehicleFilterHistoryTripOutput;

// SSE uses fetchSseApi for streaming, reconnection and shared connections.
export const zoneStatusStreamRequest = () => Promise.resolve({ url: '/api/tms/tmszonestatus/stream?refresh=2' });
export const getZoneStatusSnapshot = () =>
  getAPI(TmsZoneStatusApi).apiTmsTmszonestatusSnapshotGet();
export const getVehicleFilterHistory = (input: VehicleFilterHistoryInput) =>
  getAPI(TmsVehicleFilterHistoryApi).apiTmsTmsvehiclefilterhistoryWindowpagePost(input);
/** Lịch sử lọc xe gom theo chuyến (các lượt cùng biển đi tiến theo tuyến), mới nhất trước. */
export const getVehicleFilterTrips = (input: VehicleFilterTripInput) =>
  getAPI(TmsVehicleFilterHistoryApi).apiTmsTmsvehiclefilterhistoryTrippagePost(input);
